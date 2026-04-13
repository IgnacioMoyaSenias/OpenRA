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

public sealed class ChallengeOrderTranslator
{
	readonly World world;
	readonly Player player;

	public ChallengeOrderTranslator(World world, Player player)
	{
		this.world = world;
		this.player = player;
	}

	public Order TranslateAction(ChallengeAction action)
	{
		if (action == null || string.IsNullOrEmpty(action.Kind))
			return null;

		try
		{
			switch (action.Kind)
			{
				case "move":
					return TranslateMoveAction(action);
				case "attack":
					return TranslateAttackAction(action);
				case "build":
					return TranslateBuildAction(action);
				case "harvest":
					return TranslateHarvestAction(action);
				case "set_rally":
					return TranslateSetRallyAction(action);
				case "sell":
					return TranslateSellAction(action);
				default:
					Console.WriteLine($"[Challenge] Unknown action type: {action.Kind}");
					return null;
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[Challenge] Error translating action {action.Kind}: {ex.Message}");
			return null;
		}
	}

	Order TranslateMoveAction(ChallengeAction action)
	{
		var unitId = action.UnitId;
		var target = action.Target ?? action.TargetPos;

		if (string.IsNullOrEmpty(unitId) || target == null)
		{
			Console.WriteLine($"[Challenge] Move action requires unit_id and target");
			return null;
		}

		if (!int.TryParse(unitId, out var actorId))
		{
			Console.WriteLine($"[Challenge] Invalid unit_id format: {unitId}");
			return null;
		}

		var actor = world.Actors.FirstOrDefault(a => a.ActorID == actorId);
		if (actor == null || actor.Owner != player)
		{
			Console.WriteLine($"[Challenge] Unit {unitId} not found or not owned by player");
			return null;
		}

		var targetCell = new CPos(target.X, target.Y);
		var targetWorldPos = world.Map.CenterOfCell(targetCell);

		// Create move order
		return new Order("Move", actor, Target.FromPos(targetWorldPos), false);
	}

	Order TranslateAttackAction(ChallengeAction action)
	{
		var unitId = action.UnitId;
		var targetActorId = action.TargetActorId;
		var targetPos = action.Target ?? action.TargetPos;

		if (string.IsNullOrEmpty(unitId))
		{
			Console.WriteLine($"[Challenge] Attack action requires unit_id");
			return null;
		}

		if (!int.TryParse(unitId, out var actorId))
		{
			Console.WriteLine($"[Challenge] Invalid unit_id format: {unitId}");
			return null;
		}

		var actor = world.Actors.FirstOrDefault(a => a.ActorID == actorId);
		if (actor == null || actor.Owner != player)
		{
			Console.WriteLine($"[Challenge] Unit {unitId} not found or not owned by player");
			return null;
		}

		// Check if unit can attack
		var attack = actor.TraitOrDefault<AttackBase>();
		if (attack == null)
		{
			Console.WriteLine($"[Challenge] Unit {unitId} cannot attack");
			return null;
		}

		// Prefer target actor over position
		if (!string.IsNullOrEmpty(targetActorId))
		{
			if (!int.TryParse(targetActorId, out var targetId))
			{
				Console.WriteLine($"[Challenge] Invalid target_actor_id format: {targetActorId}");
				return null;
			}

			var targetActor = world.Actors.FirstOrDefault(a => a.ActorID == targetId);
			if (targetActor == null)
			{
				Console.WriteLine($"[Challenge] Target actor {targetActorId} not found");
				return null;
			}

			return new Order("Attack", actor, Target.FromActor(targetActor), false);
		}
		else if (targetPos != null)
		{
			var targetCell = new CPos(targetPos.X, targetPos.Y);
			var targetWorldPos = world.Map.CenterOfCell(targetCell);
			return new Order("Attack", actor, Target.FromPos(targetWorldPos), false);
		}

		Console.WriteLine($"[Challenge] Attack action requires target_actor_id or target position");
		return null;
	}

	Order TranslateBuildAction(ChallengeAction action)
	{
		var producerId = action.ProducerId;
		var unitType = action.UnitType;
		var target = action.Target ?? action.TargetPos;

		if (string.IsNullOrEmpty(producerId) || string.IsNullOrEmpty(unitType))
		{
			Console.WriteLine($"[Challenge] Build action requires producer_id and unit_type");
			return null;
		}

		if (!int.TryParse(producerId, out var actorId))
		{
			Console.WriteLine($"[Challenge] Invalid producer_id format: {producerId}");
			return null;
		}

		var producer = world.Actors.FirstOrDefault(a => a.ActorID == actorId);
		if (producer == null || producer.Owner != player)
		{
			Console.WriteLine($"[Challenge] Producer {producerId} not found or not owned by player");
			return null;
		}

		// Check if producer can build
		var production = producer.TraitOrDefault<Production>();
		if (production == null)
		{
			Console.WriteLine($"[Challenge] Producer {producerId} cannot produce units");
			return null;
		}

		// Find the unit type in the rules
		var unitInfo = world.Map.Rules.Actors.Values.FirstOrDefault(a => a.Name == unitType);
		if (unitInfo == null)
		{
			Console.WriteLine($"[Challenge] Unknown unit type: {unitType}");
			return null;
		}

		// Check if player can build this unit type
		var productionQueue = producer.TraitOrDefault<ProductionQueue>();
		if (productionQueue == null)
		{
			Console.WriteLine($"[Challenge] Producer {producerId} does not have production queue");
			return null;
		}

		var buildable = productionQueue.AllItems().FirstOrDefault(q => q.Name == unitType);
		if (buildable == null)
		{
			Console.WriteLine($"[Challenge] Unit type {unitType} not available for production");
			return null;
		}

		// Create production order
		return Order.StartProduction(producer, unitType, 1);
	}

	Order TranslateHarvestAction(ChallengeAction action)
	{
		var unitId = action.UnitId;
		var targetResourceId = action.TargetResourceId;
		var targetPos = action.Target ?? action.TargetPos;

		if (string.IsNullOrEmpty(unitId))
		{
			Console.WriteLine($"[Challenge] Harvest action requires unit_id");
			return null;
		}

		if (!int.TryParse(unitId, out var actorId))
		{
			Console.WriteLine($"[Challenge] Invalid unit_id format: {unitId}");
			return null;
		}

		var actor = world.Actors.FirstOrDefault(a => a.ActorID == actorId);
		if (actor == null || actor.Owner != player)
		{
			Console.WriteLine($"[Challenge] Unit {unitId} not found or not owned by player");
			return null;
		}

		// Check if unit can harvest
		var harvest = actor.TraitOrDefault<Harvester>();
		if (harvest == null)
		{
			Console.WriteLine($"[Challenge] Unit {unitId} cannot harvest");
			return null;
		}

		// Target resource or position
		if (!string.IsNullOrEmpty(targetResourceId))
		{
			if (!int.TryParse(targetResourceId, out var resourceId))
			{
				Console.WriteLine($"[Challenge] Invalid target_resource_id format: {targetResourceId}");
				return null;
			}

			var resourceActor = world.Actors.FirstOrDefault(a => a.ActorID == resourceId);
			if (resourceActor == null)
			{
				Console.WriteLine($"[Challenge] Resource {targetResourceId} not found");
				return null;
			}

			return new Order("Harvest", actor, Target.FromActor(resourceActor), false);
		}
		else if (targetPos != null)
		{
			var targetCell = new CPos(targetPos.X, targetPos.Y);
			var targetWorldPos = world.Map.CenterOfCell(targetCell);
			return new Order("Harvest", actor, Target.FromPos(targetWorldPos), false);
		}

		Console.WriteLine($"[Challenge] Harvest action requires target_resource_id or target position");
		return null;
	}

	Order TranslateSetRallyAction(ChallengeAction action)
	{
		var factoryId = action.FactoryId;
		var target = action.Target ?? action.TargetPos;

		if (string.IsNullOrEmpty(factoryId) || target == null)
		{
			Console.WriteLine($"[Challenge] Set rally action requires factory_id and target");
			return null;
		}

		if (!int.TryParse(factoryId, out var actorId))
		{
			Console.WriteLine($"[Challenge] Invalid factory_id format: {factoryId}");
			return null;
		}

		var factory = world.Actors.FirstOrDefault(a => a.ActorID == actorId);
		if (factory == null || factory.Owner != player)
		{
			Console.WriteLine($"[Challenge] Factory {factoryId} not found or not owned by player");
			return null;
		}

		// Check if factory has rally point capability
		var rally = factory.TraitOrDefault<RallyPoint>();
		if (rally == null)
		{
			Console.WriteLine($"[Challenge] Factory {factoryId} does not support rally points");
			return null;
		}

		var targetCell = new CPos(target.X, target.Y);
		var targetWorldPos = world.Map.CenterOfCell(targetCell);

		return new Order("SetRallyPoint", factory, Target.FromPos(targetWorldPos), false);
	}

	Order TranslateSellAction(ChallengeAction action)
	{
		var buildingId = action.BuildingId;

		if (string.IsNullOrEmpty(buildingId))
		{
			Console.WriteLine($"[Challenge] Sell action requires building_id");
			return null;
		}

		if (!int.TryParse(buildingId, out var actorId))
		{
			Console.WriteLine($"[Challenge] Invalid building_id format: {buildingId}");
			return null;
		}

		var building = world.Actors.FirstOrDefault(a => a.ActorID == actorId);
		if (building == null || building.Owner != player)
		{
			Console.WriteLine($"[Challenge] Building {buildingId} not found or not owned by player");
			return null;
		}

		// Check if building can be sold
		var sell = building.TraitOrDefault<Sellable>();
		if (sell == null)
		{
			Console.WriteLine($"[Challenge] Building {buildingId} cannot be sold");
			return null;
		}

		return new Order("Sell", building, Target.FromActor(building), false);
	}
}
