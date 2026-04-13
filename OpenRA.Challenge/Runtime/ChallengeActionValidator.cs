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
using OpenRA;
using OpenRA.Mods.Challenge.Protocol;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Challenge.Runtime;

public sealed class ChallengeActionValidator
{
	readonly World world;
	readonly Player player;
	readonly HashSet<string> enabledActionTypes;
	readonly Dictionary<int, int> validDecisionIds;

	public ChallengeActionValidator(World world, Player player)
	{
		this.world = world;
		this.player = player;

		enabledActionTypes = new HashSet<string>
		{
			"move",
			"attack",
			"build",
			"harvest",
			"set_rally",
			"sell"
		};

		validDecisionIds = new Dictionary<int, int>();
	}

	public void UpdateValidDecisionId(int decisionId)
	{
		validDecisionIds[decisionId] = decisionId;
	}

	public void CleanupOldDecisionIds(int currentDecisionId)
	{
		var oldIds = validDecisionIds.Keys.Where(id => id < currentDecisionId - 10).ToList();
		foreach (var oldId in oldIds)
			validDecisionIds.Remove(oldId);
	}

	public ChallengeValidationResult ValidateAction(ChallengeAction action, int maxActionsPerDecision)
	{
		if (action == null)
			return new ChallengeValidationResult { IsValid = false, Error = "Action is null" };

		// Validate schema
		var schemaResult = ValidateSchema(action);
		if (!schemaResult.IsValid)
			return schemaResult;

		// Decision ID validation happens at batch level

		// Validate action type is enabled
		var typeResult = ValidateActionType(action.Kind);
		if (!typeResult.IsValid)
			return typeResult;

		// Validate actor ownership
		var ownershipResult = ValidateActorOwnership(action);
		if (!ownershipResult.IsValid)
			return ownershipResult;

		// Validate coordinates
		var coordResult = ValidateCoordinates(action);
		if (!coordResult.IsValid)
			return coordResult;

		// Validate target visibility when required
		var visibilityResult = ValidateTargetVisibility(action);
		if (!visibilityResult.IsValid)
			return visibilityResult;

		// Validate maximum actions per decision
		var maxActionsResult = ValidateMaxActions(action, maxActionsPerDecision);
		if (!maxActionsResult.IsValid)
			return maxActionsResult;

		return new ChallengeValidationResult { IsValid = true };
	}

	ChallengeValidationResult ValidateSchema(ChallengeAction action)
	{
		if (string.IsNullOrEmpty(action.Kind))
			return new ChallengeValidationResult { IsValid = false, Error = "Action type is required" };

		return new ChallengeValidationResult { IsValid = true };
	}

	ChallengeValidationResult ValidateDecisionId(int decisionId)
	{
		if (!validDecisionIds.ContainsKey(decisionId))
			return new ChallengeValidationResult { IsValid = false, Error = $"Decision ID {decisionId} is not valid or expired" };

		return new ChallengeValidationResult { IsValid = true };
	}

	ChallengeValidationResult ValidateActionType(string actionType)
	{
		if (!enabledActionTypes.Contains(actionType))
			return new ChallengeValidationResult { IsValid = false, Error = $"Action type '{actionType}' is not enabled" };

		return new ChallengeValidationResult { IsValid = true };
	}

	ChallengeValidationResult ValidateActorOwnership(ChallengeAction action)
	{
		var actorId = action.UnitId ?? action.ProducerId ?? action.FactoryId ?? action.BuildingId;

		if (string.IsNullOrEmpty(actorId))
			return new ChallengeValidationResult { IsValid = true }; // Some actions don't require actor

		if (!int.TryParse(actorId, out var id))
			return new ChallengeValidationResult { IsValid = false, Error = $"Invalid actor ID format: {actorId}" };

		var actor = world.Actors.FirstOrDefault(a => a.ActorID == id);
		if (actor == null)
			return new ChallengeValidationResult { IsValid = false, Error = $"Actor {actorId} not found" };

		if (actor.Owner != player)
			return new ChallengeValidationResult { IsValid = false, Error = $"Actor {actorId} is not owned by player {player.PlayerName}" };

		return new ChallengeValidationResult { IsValid = true };
	}

	ChallengeValidationResult ValidateCoordinates(ChallengeAction action)
	{
		var target = action.Target ?? action.TargetPos;
		if (target != null)
		{
			var x = target.X;
			var y = target.Y;

			if (x < 0 || y < 0)
				return new ChallengeValidationResult { IsValid = false, Error = "Target coordinates cannot be negative" };

			if (x >= world.Map.MapSize.Width || y >= world.Map.MapSize.Height)
				return new ChallengeValidationResult { IsValid = false, Error = "Target coordinates are outside map bounds" };
		}

		return new ChallengeValidationResult { IsValid = true };
	}

	ChallengeValidationResult ValidateTargetVisibility(ChallengeAction action)
	{
		// Only validate visibility for actions that require visible targets
		if (action.Kind == "attack")
		{
			var target = action.Target ?? action.TargetPos;
			if (target != null)
			{
				var targetCell = new CPos(target.X, target.Y);
				if (!player.Shroud.IsVisible(world.Map.CenterOfCell(targetCell)))
					return new ChallengeValidationResult { IsValid = false, Error = "Attack target is not visible" };
			}
		}

		return new ChallengeValidationResult { IsValid = true };
	}

	ChallengeValidationResult ValidateMaxActions(ChallengeAction action, int maxActionsPerDecision)
	{
		// This would be handled at the batch level, but we can validate individual action counts
		// For now, we'll assume this is handled by the caller
		return new ChallengeValidationResult { IsValid = true };
	}

	public ChallengeValidationResult ValidateActionBatch(ChallengeActionBatch actions, int maxActionsPerDecision)
	{
		if (actions == null)
			return new ChallengeValidationResult { IsValid = false, Error = "Action batch is null" };

		if (actions.Actions == null || actions.Actions.Count == 0)
			return new ChallengeValidationResult { IsValid = false, Error = "Action batch is empty" };

		if (actions.Actions.Count > maxActionsPerDecision)
			return new ChallengeValidationResult { IsValid = false, Error = $"Too many actions: {actions.Actions.Count} > {maxActionsPerDecision}" };

		// Validate decision_id
		var decisionResult = ValidateDecisionId(actions.DecisionId);
		if (!decisionResult.IsValid)
			return decisionResult;

		// Validate all actions in the batch
		foreach (var action in actions.Actions)
		{
			var result = ValidateAction(action, maxActionsPerDecision);
			if (!result.IsValid)
				return result;
		}

		return new ChallengeValidationResult { IsValid = true };
	}
}

public class ChallengeValidationResult
{
	public bool IsValid { get; init; }
	public string Error { get; init; }
}
