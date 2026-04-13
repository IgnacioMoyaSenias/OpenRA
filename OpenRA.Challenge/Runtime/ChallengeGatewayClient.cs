#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using OpenRA.Mods.Challenge.Protocol;

namespace OpenRA.Mods.Challenge.Runtime;

public sealed class ChallengeGatewayClient : IDisposable
{
	readonly string matchId;
	readonly string playerId;
	readonly int maxLineBytes;
	readonly Channel<string> outgoingLines;
	readonly ConcurrentDictionary<int, ChallengeActionBatch> actionsByDecision = [];
	readonly ConcurrentDictionary<int, int> observationTickByDecision = [];
	readonly ChallengeDecisionTracker decisionTracker = new();
	readonly CancellationTokenSource cts = new();

	TcpClient tcpClient;
	TcpClient tcpClient2;
	NetworkStream stream;
	NetworkStream stream2;
	Task readLoopTask;
	Task readLoopTask2;
	Task writeLoopTask;
	Task writeLoopTask2;
	bool useDualPorts = false;

	public ChallengeGatewayClient(string matchId, string playerId, int maxLineBytes = 64 * 1024)
	{
		this.matchId = matchId ?? throw new ArgumentNullException(nameof(matchId));
		this.playerId = playerId ?? throw new ArgumentNullException(nameof(playerId));
		this.maxLineBytes = maxLineBytes > 0 ? maxLineBytes : throw new ArgumentOutOfRangeException(nameof(maxLineBytes));
		outgoingLines = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
		{
			SingleReader = true,
			SingleWriter = false,
			AllowSynchronousContinuations = false
		});
	}

	public bool IsConnected => (tcpClient is { Connected: true }) && (!useDualPorts || tcpClient2 is { Connected: true });

	public Task ConnectAsync(string endpoint, CancellationToken cancellationToken = default)
	{
		var (host, port) = ParseLoopbackEndpoint(endpoint);
		tcpClient = new TcpClient();
		return ConnectAndStartLoopsAsync(host, port, cancellationToken);
	}

	public Task ConnectDualAsync(string endpoint1, string endpoint2, CancellationToken cancellationToken = default)
	{
		var (host1, port1) = ParseLoopbackEndpoint(endpoint1);
		var (host2, port2) = ParseLoopbackEndpoint(endpoint2);
		tcpClient = new TcpClient();
		tcpClient2 = new TcpClient();
		useDualPorts = true;
		return ConnectAndStartDualLoopsAsync(host1, port1, host2, port2, cancellationToken);
	}

	public void EnqueueInit(ChallengeInit initMessage)
	{
		initMessage.Validate();
		EnqueueLine(initMessage);
	}

	public void EnqueueObservation(ChallengeObservation observation)
	{
		observation.Validate();
		observationTickByDecision[observation.DecisionId] = observation.Tick;
		EnqueueLine(observation);
	}

	public void EnqueueEnd(ChallengeEnd endMessage)
	{
		endMessage.Validate();
		EnqueueLine(endMessage);
	}

	public bool TryTakeActions(int decisionId, int worldTick, int responseDeadlineTicks, out ChallengeActionBatch batch)
	{
		batch = null;
		if (!actionsByDecision.TryRemove(decisionId, out var candidate))
			return false;

		if (!observationTickByDecision.TryGetValue(decisionId, out var observationTick))
			return false;

		if (worldTick - observationTick > responseDeadlineTicks)
			return false;

		batch = candidate;
		return true;
	}

	public void ExpireTimedOutActions(int worldTick, int responseDeadlineTicks)
	{
		foreach (var kv in observationTickByDecision)
		{
			if (worldTick - kv.Value <= responseDeadlineTicks)
				continue;

			actionsByDecision.TryRemove(kv.Key, out _);
		}
	}

	public void Dispose()
	{
		cts.Cancel();
		outgoingLines.Writer.TryComplete();

		try { readLoopTask?.Wait(TimeSpan.FromSeconds(1)); } catch { }
		try { writeLoopTask?.Wait(TimeSpan.FromSeconds(1)); } catch { }
		try { readLoopTask2?.Wait(TimeSpan.FromSeconds(1)); } catch { }
		try { writeLoopTask2?.Wait(TimeSpan.FromSeconds(1)); } catch { }

		stream?.Dispose();
		stream2?.Dispose();
		tcpClient?.Dispose();
		tcpClient2?.Dispose();
		cts.Dispose();
	}

	async Task ConnectAndStartLoopsAsync(string host, int port, CancellationToken cancellationToken)
	{
		await tcpClient.ConnectAsync(host, port, cancellationToken);
		stream = tcpClient.GetStream();

		readLoopTask = Task.Run(() => ReadLoopAsync(cts.Token), CancellationToken.None);
		writeLoopTask = Task.Run(() => WriteLoopAsync(cts.Token), CancellationToken.None);
	}

	async Task ConnectAndStartDualLoopsAsync(string host1, int port1, string host2, int port2, CancellationToken cancellationToken)
	{
		await Task.WhenAll(
			tcpClient.ConnectAsync(host1, port1, cancellationToken).AsTask(),
			tcpClient2.ConnectAsync(host2, port2, cancellationToken).AsTask()
		);

		stream = tcpClient.GetStream();
		stream2 = tcpClient2.GetStream();

		readLoopTask = Task.Run(() => ReadLoopAsync(cts.Token), CancellationToken.None);
		writeLoopTask = Task.Run(() => WriteLoopAsync(cts.Token), CancellationToken.None);
		readLoopTask2 = Task.Run(() => ReadLoopAsync2(cts.Token), CancellationToken.None);
		writeLoopTask2 = Task.Run(() => WriteLoopAsync2(cts.Token), CancellationToken.None);
	}

	void EnqueueLine<T>(T message)
	{
		var line = ChallengeJsonlProtocol.SerializeLine(message);
		outgoingLines.Writer.TryWrite(line);
	}

	async Task WriteLoopAsync(CancellationToken cancellationToken)
	{
		try
		{
			await foreach (var line in outgoingLines.Reader.ReadAllAsync(cancellationToken))
			{
				ChallengeJsonlProtocol.WriteUtf8Line(stream, line);
				await stream.FlushAsync(cancellationToken);
			}
		}
		catch (OperationCanceledException) { }
		catch (Exception)
		{
			cts.Cancel();
		}
	}

	async Task WriteLoopAsync2(CancellationToken cancellationToken)
	{
		try
		{
			await foreach (var line in outgoingLines.Reader.ReadAllAsync(cancellationToken))
			{
				ChallengeJsonlProtocol.WriteUtf8Line(stream2, line);
				await stream2.FlushAsync(cancellationToken);
			}
		}
		catch (OperationCanceledException) { }
		catch (Exception)
		{
			cts.Cancel();
		}
	}

	async Task ReadLoopAsync(CancellationToken cancellationToken)
	{
		try
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				var line = await Task.Run(() => ChallengeJsonlProtocol.ReadUtf8Line(stream, maxLineBytes), cancellationToken);
				if (line == null)
					break;

				ChallengeActionBatch batch;
				try
				{
					batch = ChallengeJsonlProtocol.DeserializeLine<ChallengeActionBatch>(line);
					batch.Validate(int.MaxValue);
				}
				catch
				{
					continue;
				}

				if (!string.Equals(batch.MatchId, matchId, StringComparison.Ordinal) ||
					!string.Equals(batch.PlayerId, playerId, StringComparison.Ordinal))
					continue;

				decisionTracker.ValidateMonotonicAndStable(batch.DecisionId, line);
				actionsByDecision[batch.DecisionId] = batch;
			}
		}
		catch (OperationCanceledException) { }
		catch (Exception)
		{
			cts.Cancel();
		}
	}

	async Task ReadLoopAsync2(CancellationToken cancellationToken)
	{
		try
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				var line = await Task.Run(() => ChallengeJsonlProtocol.ReadUtf8Line(stream2, maxLineBytes), cancellationToken);
				if (line == null)
					break;

				ChallengeActionBatch batch;
				try
				{
					batch = ChallengeJsonlProtocol.DeserializeLine<ChallengeActionBatch>(line);
					batch.Validate(int.MaxValue);
				}
				catch
				{
					continue;
				}

				if (!string.Equals(batch.MatchId, matchId, StringComparison.Ordinal) ||
					!string.Equals(batch.PlayerId, playerId, StringComparison.Ordinal))
					continue;

				decisionTracker.ValidateMonotonicAndStable(batch.DecisionId, line);
				actionsByDecision[batch.DecisionId] = batch;
			}
		}
		catch (OperationCanceledException) { }
		catch (Exception)
		{
			cts.Cancel();
		}
	}

	public static (string Host, int Port) ParseLoopbackEndpoint(string endpoint)
	{
		if (string.IsNullOrWhiteSpace(endpoint) || !endpoint.StartsWith("tcp:127.0.0.1:", StringComparison.Ordinal))
			throw new ChallengeProtocolException("Only loopback tcp endpoints are supported. Expected tcp:127.0.0.1:PORT.");

		var portText = endpoint["tcp:127.0.0.1:".Length..];
		if (!int.TryParse(portText, NumberStyles.None, CultureInfo.InvariantCulture, out var port) || port is <= 0 or > 65535)
			throw new ChallengeProtocolException($"Invalid tcp loopback port in endpoint '{endpoint}'.");

		return ("127.0.0.1", port);
	}
}
