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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using OpenRA;
using OpenRA.Mods.Common.Traits;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Challenge.Traits.World
{
	public sealed class ChallengeResultReporter : INotifyWinStateChanged, INotifyGameLoaded, ITick, INotifyActorDisposing
	{
		readonly ChallengeResultReporterInfo info;
		readonly OpenRA.World gameWorld;
		readonly string resultsDirectory;
		readonly string resultsPath;

		// Game state tracking
		GameState gameState = GameState.InProgress;
		int gameStartTick;
		int gameEndTick;
		Player winner;
		int timeouts = 0;
		int invalidActions = 0;
		List<string> metrics = new();

		// Challenge-specific tracking
		string matchId;
		string mapUid;
		int seed;

		public ChallengeResultReporter(Actor self, ChallengeResultReporterInfo info)
		{
			this.info = info;
			this.gameWorld = self.World;

			resultsDirectory = info.ResultsDirectory;
			resultsPath = Path.Combine(resultsDirectory, "result.json");
		}

		void INotifyGameLoaded.GameLoaded(OpenRA.World w)
		{
			gameStartTick = w.WorldTick;
			mapUid = w.Map.Uid;
			seed = w.LocalRandom.Next();

			// Get match ID from settings or generate one
			matchId = string.IsNullOrWhiteSpace(Game.Settings.Challenge.MatchId)
				? $"match-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
				: Game.Settings.Challenge.MatchId;

			// Ensure results directory exists
			Directory.CreateDirectory(resultsDirectory);

			// Log game start
			Console.WriteLine($"[Challenge] Game started - Match: {matchId}, Map: {w.Map.Title}, Seed: {seed}");
		}

		void INotifyWinStateChanged.OnPlayerWon(Player winner)
		{
			this.winner = winner;
			gameState = GameState.Finished;
			gameEndTick = gameWorld.WorldTick;

			Console.WriteLine($"[Challenge] Game finished - Winner: {winner.PlayerName}, Ticks: {gameEndTick - gameStartTick}");
			SaveResults();
		}

		void INotifyWinStateChanged.OnPlayerLost(Player loser)
		{
			// Handle player loss if needed
		}

		void ITick.Tick(Actor self)
		{
			if (info.MetricsInterval <= 0 || gameState != GameState.InProgress)
				return;

			// Collect metrics at intervals
			if (gameWorld.WorldTick % info.MetricsInterval == 0)
			{
				CollectMetrics();
			}
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			// Save results if game wasn't finished normally
			if (gameState == GameState.InProgress)
			{
				gameEndTick = gameWorld.WorldTick;
				gameState = GameState.Interrupted;
				SaveResults();
			}

			// Save metrics if enabled
			if (info.SaveMetrics && metrics.Count > 0)
			{
				SaveMetrics();
			}
		}

		void CollectMetrics()
		{
			var tick = gameWorld.WorldTick;
			var playerMetrics = new Dictionary<string, object>();

			foreach (var player in gameWorld.Players)
			{
				if (player.Playable && player.PlayerActor != null)
				{
					var playerResources = player.PlayerActor.TraitOrDefault<PlayerResources>();
					var powerManager = player.PlayerActor.TraitOrDefault<PowerManager>();

					var playerName = player.InternalName;
					playerMetrics[playerName] = new
					{
						tick = tick,
						player_id = playerName,
						cash = playerResources?.Cash ?? 0,
						power_provided = powerManager?.PowerProvided ?? 0,
						power_drained = powerManager?.PowerDrained ?? 0,
						unit_count = gameWorld.Actors.Count(a => a.Owner == player && a.IsInWorld && !a.IsDead),
						structure_count = gameWorld.Actors.Count(a => a.Owner == player && a.IsInWorld && !a.IsDead && a.Info.HasTraitInfo<BuildingInfo>())
					};

					var metricLine = JsonSerializer.Serialize(playerMetrics[playerName]);
					metrics.Add(metricLine);
				}
			}
		}

		void SaveResults()
		{
			try
			{
				var result = new
				{
					match_id = matchId,
					map = new
					{
						uid = mapUid,
						title = gameWorld.Map.Title,
						author = gameWorld.Map.Author,
						width = gameWorld.Map.MapSize.Width,
						height = gameWorld.Map.MapSize.Height
					},
					seed = seed,
					winner = winner?.PlayerName,
					ticks = gameEndTick - gameStartTick,
					replay_path = GetReplayPath(),
					timeouts = timeouts,
					invalid_actions = invalidActions,
					status = GetGameStatus(),
					timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
				};

				var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
				File.WriteAllText(resultsPath, json);

				Console.WriteLine($"[Challenge] Results saved to: {resultsPath}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[Challenge] Error saving results: {ex.Message}");
			}
		}

		void SaveMetrics()
		{
			try
			{
				var metricsPath = Path.Combine(resultsDirectory, "metrics.jsonl");
				File.WriteAllLines(metricsPath, metrics);

				Console.WriteLine($"[Challenge] Metrics saved to: {metricsPath} ({metrics.Count} entries)");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[Challenge] Error saving metrics: {ex.Message}");
			}
		}

		string GetReplayPath()
		{
			if (!info.SaveReplay)
				return null;

			// Try to find the replay file for this match
			var replayDir = Path.Combine(Platform.SupportDir, "Replays", "Multiplayer");
			if (!Directory.Exists(replayDir))
				return null;

			var replayFiles = Directory.GetFiles(replayDir, "*.orarep")
				.OrderByDescending(f => File.GetCreationTime(f))
				.ToArray();

			// Find the most recent replay that matches our match time
			foreach (var replayFile in replayFiles)
			{
				var fileTime = File.GetCreationTime(replayFile);
				var gameStartTime = DateTimeOffset.FromUnixTimeMilliseconds(gameStartTick * gameWorld.Timestep);

				// If the replay was created within 1 minute of game start, it's likely ours
				if (Math.Abs((fileTime - gameStartTime).TotalMinutes) < 1)
				{
					return replayFile;
				}
			}

			return null;
		}

		string GetGameStatus()
		{
			return gameState switch
			{
				GameState.Finished => "finished",
				GameState.Interrupted => "interrupted",
				GameState.InProgress => "in_progress",
				_ => "unknown"
			};
		}

		public void IncrementTimeouts()
		{
			timeouts++;
		}

		public void IncrementInvalidActions()
		{
			invalidActions++;
		}

		enum GameState
		{
			InProgress,
			Finished,
			Interrupted
		}
	}
}
