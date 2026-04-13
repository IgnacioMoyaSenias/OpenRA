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
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Challenge.Protocol;
using OpenRA.Mods.Challenge.Runtime;
using OpenRA.Traits;

namespace OpenRA.Mods.Challenge.Traits
{
	public sealed class ExternalAgentBotModule : ConditionalTrait<ExternalAgentBotModuleInfo>, IBotTick, INotifyWinStateChanged, INotifyActorDisposing
	{
		readonly Actor self;
		readonly ExternalAgentBotModuleInfo info;
		readonly Dictionary<int, int> decisionTicks = [];

		ChallengeGatewayClient gatewayClient;
		ChallengeActionValidator actionValidator;
		ChallengeOrderTranslator orderTranslator;
		System.Threading.Tasks.Task connectTask;
		bool connectFailed;
		bool initSent;
		int nextDecisionIdToSend;
		int nextDecisionIdToApply;
		int nextDecisionTick;
		string matchId;
		string playerId;
		static readonly object portAssignmentLock = new object();
		static readonly HashSet<int> assignedPorts = [];
		int assignedPort = -1;

		public List<ChallengeVisibleEnemy> VisibleEnemies { get; set; } = [];

		public ExternalAgentBotModule(Actor self, ExternalAgentBotModuleInfo info)
			: base(info)
		{
			this.self = self;
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (IsTraitDisabled || !Game.Settings.Challenge.Enabled)
				return;

			var worldTick = bot.Player.World.WorldTick;
			if (!EnsureConnected(bot))
				return;

			if (!initSent)
			{
				gatewayClient.EnqueueInit(BuildInit(bot));
				initSent = true;
				nextDecisionTick = worldTick;
			}

			TryApplyPendingActions(bot, worldTick);
			gatewayClient.ExpireTimedOutActions(worldTick, Info.ResponseDeadlineTicks);

			if (worldTick < nextDecisionTick)
				return;

			var decisionId = nextDecisionIdToSend++;
			var observation = BuildObservation(bot, worldTick, decisionId);
			gatewayClient.EnqueueObservation(observation);
			decisionTicks[decisionId] = worldTick;
			nextDecisionTick = worldTick + Math.Max(1, Info.DecisionInterval);
		}

		bool EnsureConnected(IBot bot)
		{
			if (connectFailed)
				return false;

			if (gatewayClient != null && gatewayClient.IsConnected)
				return true;

			if (gatewayClient == null)
			{
				matchId = string.IsNullOrWhiteSpace(Game.Settings.Challenge.MatchId)
					? $"m-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
					: Game.Settings.Challenge.MatchId;
				playerId = bot.Player.InternalName;
				gatewayClient = new ChallengeGatewayClient(matchId, playerId);
				actionValidator = new ChallengeActionValidator(bot.Player.World, bot.Player);
				orderTranslator = new ChallengeOrderTranslator(bot.Player.World, bot.Player);

				if (Game.Settings.Challenge.DualPortMode)
				{
					// In dual-port mode, all bots share the same two ports
					var endpoint1 = Game.Settings.Challenge.AgentGatewayEndpoint;
					var endpoint2 = Game.Settings.Challenge.AgentGatewayEndpoint2;

					// Just verify both endpoints are valid and gateway is listening
					if (!ValidateEndpoint(endpoint1) || !ValidateEndpoint(endpoint2))
					{
						connectFailed = true;
						return false;
					}

					connectTask = gatewayClient.ConnectDualAsync(endpoint1, endpoint2);
					connectTask.ContinueWith(t => { }, System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);
				}
				else
				{
					if (!TryAssignPort(bot, Game.Settings.Challenge.AgentGatewayEndpoint, out var endpoint))
					{
						connectFailed = true;
						return false;
					}
					connectTask = gatewayClient.ConnectAsync(endpoint);
				}
			}

			if (connectTask == null)
				return gatewayClient.IsConnected;

			if (connectTask.IsFaulted || connectTask.IsCanceled)
			{
				connectFailed = true;
				return false;
			}

			return connectTask.IsCompletedSuccessfully && gatewayClient.IsConnected;
		}

		ChallengeInit BuildInit(IBot bot)
		{
			var world = bot.Player.World;
			return new ChallengeInit
			{
				MatchId = matchId,
				PlayerId = playerId,
				PlayerName = bot.Player.PlayerName,
				Map = new ChallengeInitMapInfo
				{
					Id = world.Map.Uid ?? "",
					Title = world.Map.Title ?? "",
					Width = world.Map.MapSize.Width,
					Height = world.Map.MapSize.Height
				},
				Mod = new ChallengeInitModInfo
				{
					Id = "challenge-ra",
					Version = "1.0.0",
					EngineVersion = "challenge-2026.04"
				},
				Faction = bot.Player.Faction?.InternalName ?? "",
				Side = $"spawn-{bot.Player.SpawnPoint}",
				Seed = (ulong)Math.Max(0, Game.Settings.Challenge.Seed),
				DecisionIntervalTicks = Info.DecisionInterval,
				ResponseDeadlineTicks = Info.ResponseDeadlineTicks,
				MaxActionsPerDecision = Info.MaxActionsPerDecision,
				Capabilities = new ChallengeCapabilities
				{
					AllowBuild = Info.AllowBuild,
					AllowSell = Info.AllowSell,
					AllowResearch = Info.AllowResearch,
					AllowSupportPowers = Info.AllowSupportPowers
				},
				VisibilityMode = Game.Settings.Challenge.StrictFogOfWar ? "strict-fow" : "partial-fow",
				TimeBudgetMsHint = 50
			};
		}

		ChallengeObservation BuildObservation(IBot bot, int worldTick, int decisionId)
		{
			var builder = new ChallengeObservationBuilder(bot.Player.World, bot.Player);
			var builtObservation = builder.Build(worldTick, decisionId);

			// Create new observation with match and player IDs
			var observation = new ChallengeObservation
			{
				Type = builtObservation.Type,
				ApiVersion = builtObservation.ApiVersion,
				MatchId = matchId,
				PlayerId = playerId,
				DecisionId = builtObservation.DecisionId,
				Tick = builtObservation.Tick,
				Resources = builtObservation.Resources,
				Scoreboard = builtObservation.Scoreboard,
				Map = builtObservation.Map,
				OwnUnits = builtObservation.OwnUnits,
				OwnStructures = builtObservation.OwnStructures,
				VisibleEnemies = builtObservation.VisibleEnemies,
				VisibleNeutralActors = builtObservation.VisibleNeutralActors,
				Events = builtObservation.Events,
				VisibilityMaskRef = builtObservation.VisibilityMaskRef
			};

			return observation;
		}

		void TryApplyPendingActions(IBot bot, int worldTick)
		{
			while (decisionTicks.TryGetValue(nextDecisionIdToApply, out var decisionTick))
			{
				if (worldTick < decisionTick + Math.Max(1, Info.DecisionInterval))
					return;

				if (gatewayClient.TryTakeActions(nextDecisionIdToApply, worldTick, Info.ResponseDeadlineTicks, out var batch))
				{
					ApplyActionBatch(bot, batch);
					decisionTicks.Remove(nextDecisionIdToApply);
					nextDecisionIdToApply++;
					continue;
				}

				if (worldTick - decisionTick > Info.ResponseDeadlineTicks)
				{
					decisionTicks.Remove(nextDecisionIdToApply);
					nextDecisionIdToApply++;
					continue;
				}

				return;
			}
		}

		void ApplyActionBatch(IBot bot, ChallengeActionBatch batch)
		{
			// Update valid decision ID for validation
			actionValidator.UpdateValidDecisionId(batch.DecisionId);

			// Validate the entire batch first
			var validationResult = actionValidator.ValidateActionBatch(batch, Info.MaxActionsPerDecision);
			if (!validationResult.IsValid)
			{
				// Log the validation error but continue processing other actions
				Console.WriteLine($"[Challenge] Invalid action batch for decision {batch.DecisionId}: {validationResult.Error}");
				return;
			}

			var maxActions = Math.Min(Info.MaxActionsPerDecision, batch.Actions.Count);
			for (var i = 0; i < maxActions; i++)
			{
				var action = batch.Actions[i];
				var actionValidation = actionValidator.ValidateAction(action, Info.MaxActionsPerDecision);

				if (!actionValidation.IsValid)
				{
					// Log individual action errors but continue with other actions
					Console.WriteLine($"[Challenge] Invalid action {action.Kind} in decision {batch.DecisionId}: {actionValidation.Error}");
					continue;
				}

				var order = orderTranslator.TranslateAction(action);
				if (order != null)
					bot.QueueOrder(order);
			}

			// Clean up old decision IDs to prevent memory leaks
			actionValidator.CleanupOldDecisionIds(batch.DecisionId);
		}

		Order TranslateActionToOrder(IBot bot, ChallengeAction action)
		{
			if (action == null || string.IsNullOrEmpty(action.Kind))
				return null;

			switch (action.Kind)
			{
				case "build":
					return BuildOrderFromBuild(bot, action);
				case "move":
					return BuildOrderFromMove(bot, action);
				case "attack":
					return BuildOrderFromAttack(bot, action);
				case "harvest":
					return BuildOrderFromHarvest(bot, action);
				case "set_rally":
					return BuildOrderFromSetRally(bot, action);
				case "sell":
					return BuildOrderFromSell(bot, action);
				default:
					return null;
			}
		}

		Order BuildOrderFromBuild(IBot bot, ChallengeAction action)
		{
			if (!Info.AllowBuild || !TryGetOwnedActor(bot, action.ProducerId, out var producer) || string.IsNullOrWhiteSpace(action.UnitType))
				return null;

			return Order.StartProduction(producer, action.UnitType, 1);
		}

		Order BuildOrderFromMove(IBot bot, ChallengeAction action)
		{
			if (action.Target == null || action.UnitIds == null || action.UnitIds.Count == 0)
				return null;

			var actor = ResolveFirstOwnedActor(bot, action.UnitIds);
			return actor == null ? null : new Order("Move", actor, Target.FromCell(bot.Player.World, new CPos(action.Target.X, action.Target.Y)), false);
		}

		Order BuildOrderFromAttack(IBot bot, ChallengeAction action)
		{
			if (action.UnitIds == null || action.UnitIds.Count == 0)
				return null;

			var actor = ResolveFirstOwnedActor(bot, action.UnitIds);
			if (actor == null)
				return null;

			if (!string.IsNullOrWhiteSpace(action.TargetActorId) && TryGetAnyActor(bot, action.TargetActorId, out var targetActor))
				return new Order("Attack", actor, Target.FromActor(targetActor), false);

			if (action.TargetPos != null)
				return new Order("AttackMove", actor, Target.FromCell(bot.Player.World, new CPos(action.TargetPos.X, action.TargetPos.Y)), false);

			return null;
		}

		Order BuildOrderFromHarvest(IBot bot, ChallengeAction action)
		{
			if (string.IsNullOrWhiteSpace(action.UnitId))
				return null;

			if (!TryGetOwnedActor(bot, action.UnitId, out var actor))
				return null;

			var target = action.TargetPos ?? action.Target;
			return target == null ? null : new Order("Harvest", actor, Target.FromCell(bot.Player.World, new CPos(target.X, target.Y)), false);
		}

		Order BuildOrderFromSetRally(IBot bot, ChallengeAction action)
		{
			if (action.Target == null || !TryGetOwnedActor(bot, action.FactoryId, out var actor))
				return null;

			return new Order("SetRallyPoint", actor, Target.FromCell(bot.Player.World, new CPos(action.Target.X, action.Target.Y)), false);
		}

		Order BuildOrderFromSell(IBot bot, ChallengeAction action)
		{
			if (!Info.AllowSell || !TryGetOwnedActor(bot, action.BuildingId, out var actor))
				return null;

			return new Order("Sell", actor, Target.FromActor(actor), false);
		}

		Actor ResolveFirstOwnedActor(IBot bot, IEnumerable<string> actorIds)
		{
			foreach (var actorId in actorIds)
				if (TryGetOwnedActor(bot, actorId, out var actor))
					return actor;

			return null;
		}

		bool TryGetAnyActor(IBot bot, string actorId, out Actor actor)
		{
			actor = null;
			if (!uint.TryParse(actorId, out var numericId))
				return false;

			var candidate = bot.Player.World.GetActorById(numericId);
			if (candidate == null || candidate.Disposed || candidate.IsDead)
				return false;

			actor = candidate;
			return true;
		}

		bool TryGetOwnedActor(IBot bot, string actorId, out Actor actor)
		{
			actor = null;
			if (!TryGetAnyActor(bot, actorId, out var candidate))
				return false;

			if (candidate.Owner != bot.Player)
				return false;

			actor = candidate;
			return true;
		}

		void INotifyWinStateChanged.OnPlayerWon(Player winner)
		{
			SendEndMessage(winner == self.Owner ? "win" : "loss", winner?.InternalName, "win-state-changed");
		}

		void INotifyWinStateChanged.OnPlayerLost(Player loser)
		{
			SendEndMessage(loser == self.Owner ? "loss" : "win", self.Owner.World.Players.FirstOrDefault(p => p.WinState == WinState.Won)?.InternalName, "win-state-changed");
		}

		void SendEndMessage(string result, string winner, string reason)
		{
			if (gatewayClient == null || !gatewayClient.IsConnected || !initSent)
				return;

			gatewayClient.EnqueueEnd(new ChallengeEnd
			{
				MatchId = matchId ?? "",
				PlayerId = playerId ?? "",
				Result = result,
				Winner = winner,
				Ticks = self.World.WorldTick,
				Reason = reason,
				ReplayPath = "",
				ArtifactHash = "",
				Summary = new ChallengeEndSummary()
			});
		}

		bool TryAssignPort(IBot bot, string endpoint, out string assignedEndpoint)
		{
			assignedEndpoint = endpoint;

			try
			{
				var (_, port) = ChallengeGatewayClient.ParseLoopbackEndpoint(endpoint);

				lock (portAssignmentLock)
				{
					if (assignedPorts.Contains(port))
					{
						Log.Write("challenge", $"Port {port} already assigned to another bot for {bot.Player.PlayerName}");
						return false;
					}

					if (!IsPortAvailable(port))
					{
						Log.Write("challenge", $"Port {port} is not available for {bot.Player.PlayerName}");
						return false;
					}

					assignedPorts.Add(port);
					assignedPort = port;
				}

				return true;
			}
			catch (Exception ex)
			{
			Log.Write("challenge", $"Failed to parse endpoint {endpoint} for {bot.Player.PlayerName}: {ex.Message}");
				return false;
			}
		}

		bool ValidateEndpoint(string endpoint)
		{
			try
			{
				var (_, port) = ChallengeGatewayClient.ParseLoopbackEndpoint(endpoint);

				// Check if gateway is listening on this port
				using var socket = new System.Net.Sockets.TcpClient();
				socket.Connect("127.0.0.1", port);
				return true; // Gateway is listening
			}
			catch
			{
				return false; // Gateway not listening
			}
		}

		bool IsPortAvailable(int port)
		{
			try
			{
				using var socket = new System.Net.Sockets.TcpClient();
				socket.Connect("127.0.0.1", port);
				return false; // Port is in use
			}
			catch
			{
				return true; // Port is available
			}
		}

		void INotifyActorDisposing.Disposing(Actor actor)
		{
			if (assignedPort != -1)
			{
				lock (portAssignmentLock)
				{
					assignedPorts.Remove(assignedPort);
				}
			}

			gatewayClient?.Dispose();
		}
	}
}
