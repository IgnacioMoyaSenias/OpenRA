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
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenRA.Mods.Challenge.Protocol;

public static class ChallengeProtocolConstants
{
	public const string ApiVersionV1 = "v1";
	public const string InitType = "init";
	public const string ObservationType = "observation";
	public const string ActionsType = "actions";
	public const string EndType = "end";
	public const string ErrorType = "error";
	public const string HeartbeatType = "heartbeat";
}

public sealed class ChallengeProtocolException : Exception
{
	public ChallengeProtocolException(string message)
		: base(message) { }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ChallengeError
{
	[JsonPropertyName("type")]
	public string Type { get; init; } = ChallengeProtocolConstants.ErrorType;

	[JsonPropertyName("api_version")]
	public string ApiVersion { get; init; } = ChallengeProtocolConstants.ApiVersionV1;

	[JsonPropertyName("match_id")]
	public string MatchId { get; init; } = "";

	[JsonPropertyName("player_id")]
	public string PlayerId { get; init; } = "";

	[JsonPropertyName("code")]
	public string Code { get; init; } = "";

	[JsonPropertyName("message")]
	public string Message { get; init; } = "";

	[JsonPropertyName("decision_id")]
	public int? DecisionId { get; init; }

	[JsonPropertyName("fatal")]
	public bool Fatal { get; init; }

	public void Validate()
	{
		ChallengeProtocolValidation.RequireTypeAndVersion(Type, ApiVersion, ChallengeProtocolConstants.ErrorType);
		if (DecisionId is >= 0)
			return;

		if (DecisionId != null)
			throw new ChallengeProtocolException("decision_id must be non-negative.");
	}
}

public static class ChallengeProtocolValidation
{
	public static void RequireTypeAndVersion(string type, string apiVersion, string expectedType)
	{
		if (!string.Equals(type, expectedType, StringComparison.Ordinal))
			throw new ChallengeProtocolException($"Unexpected type '{type}'. Expected '{expectedType}'.");

		if (!string.Equals(apiVersion, ChallengeProtocolConstants.ApiVersionV1, StringComparison.Ordinal))
			throw new ChallengeProtocolException($"Unsupported api_version '{apiVersion}'.");
	}

	public static void RequireNonNegativeDecisionId(int decisionId)
	{
		if (decisionId < 0)
			throw new ChallengeProtocolException("decision_id must be non-negative.");
	}

	public static void RequireNonEmpty(string fieldName, string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			throw new ChallengeProtocolException($"Missing required field: {fieldName}.");
	}

	public static void RequirePositive(string fieldName, int value)
	{
		if (value <= 0)
			throw new ChallengeProtocolException($"{fieldName} must be greater than zero.");
	}
}

public sealed class ChallengeDecisionTracker
{
	int highestDecisionId = -1;
	readonly Dictionary<int, string> payloadHashes = [];

	public void ValidateMonotonicAndStable(int decisionId, string payloadHash)
	{
		ChallengeProtocolValidation.RequireNonNegativeDecisionId(decisionId);

		if (decisionId < highestDecisionId)
			throw new ChallengeProtocolException($"decision_id {decisionId} is not monotonic.");

		if (payloadHashes.TryGetValue(decisionId, out var seenHash) && !string.Equals(seenHash, payloadHash, StringComparison.Ordinal))
			throw new ChallengeProtocolException($"decision_id {decisionId} was repeated with different payload.");

		highestDecisionId = Math.Max(highestDecisionId, decisionId);
		payloadHashes[decisionId] = payloadHash;
	}
}

public static class ChallengeJsonlProtocol
{
	static readonly JsonSerializerOptions StrictJsonOptions = new()
	{
		PropertyNamingPolicy = null,
		DefaultIgnoreCondition = JsonIgnoreCondition.Never,
		UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
	};

	public static string SerializeLine<T>(T message)
	{
		var json = JsonSerializer.Serialize(message, StrictJsonOptions);
		return json + "\n";
	}

	public static T DeserializeLine<T>(string jsonLine)
	{
		if (string.IsNullOrWhiteSpace(jsonLine))
			throw new ChallengeProtocolException("Empty JSONL message.");

		var value = JsonSerializer.Deserialize<T>(jsonLine, StrictJsonOptions);
		if (value == null)
			throw new ChallengeProtocolException("Unable to deserialize JSONL message.");

		return value;
	}

	public static string ReadUtf8Line(Stream stream, int maxLineBytes)
	{
		if (maxLineBytes <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxLineBytes));

		var rented = ArrayPool<byte>.Shared.Rent(maxLineBytes);
		var count = 0;
		try
		{
			while (true)
			{
				var next = stream.ReadByte();
				if (next == -1)
				{
					if (count == 0)
						return null;

					break;
				}

				if (next == '\n')
					break;

				if (count >= maxLineBytes)
					throw new ChallengeProtocolException($"JSONL line exceeds configured maximum of {maxLineBytes} bytes.");

				rented[count++] = (byte)next;
			}

			if (count > 0 && rented[count - 1] == '\r')
				count--;

			return Encoding.UTF8.GetString(rented, 0, count);
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(rented);
		}
	}

	public static void WriteUtf8Line(Stream stream, string jsonLine)
	{
		if (jsonLine == null)
			throw new ArgumentNullException(nameof(jsonLine));

		var bytes = Encoding.UTF8.GetBytes(jsonLine);
		stream.Write(bytes, 0, bytes.Length);
	}
}
