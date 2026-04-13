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

using System.Collections.Generic;
using System.Linq;
using OpenRA;
using OpenRA.Mods.Challenge.Protocol;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Challenge.Runtime;

public sealed class ChallengeObservationBuilder
{
	readonly World world;
	readonly Player player;

	public ChallengeObservationBuilder(World world, Player player)
	{
		this.world = world;
		this.player = player;
	}

	public ChallengeObservation Build(int worldTick, int decisionId)
	{
		var playerResources = player.PlayerActor.Trait<PlayerResources>();
		var powerManager = player.PlayerActor.Trait<PowerManager>();
		var cash = playerResources.Cash + playerResources.Resources;
		var powerProvided = powerManager.PowerProvided;
		var powerConsumed = powerManager.PowerDrained;

		var ownUnits = GetOwnUnits();
		var ownStructures = GetOwnStructures();
		var visibleEnemies = GetVisibleEnemies();
		var neutralActors = GetNeutralActors();
		var events = GetRecentEvents();

		return new ChallengeObservation
		{
			Type = "observation",
			ApiVersion = "v1",
			MatchId = "", // Will be set by ExternalAgentBotModule
			PlayerId = "", // Will be set by ExternalAgentBotModule
			DecisionId = decisionId,
			Tick = worldTick,
			Resources = new ChallengeResourceSnapshot
			{
				Cash = cash,
				Power = powerProvided - powerConsumed,
				PowerUsed = powerConsumed
			},
			Scoreboard = new ChallengeScoreboardSnapshot
			{
				WinnerIfFinished = "",
				ElapsedTicks = worldTick
			},
			Map = new ChallengeObservationMapInfo
			{
				Width = world.Map.MapSize.Width,
				Height = world.Map.MapSize.Height
			},
			OwnUnits = ownUnits,
			OwnStructures = ownStructures,
			VisibleEnemies = visibleEnemies,
			VisibleNeutralActors = neutralActors,
			Events = events
		};
	}

	List<ChallengeOwnUnit> GetOwnUnits()
	{
		var ownUnits = new List<ChallengeOwnUnit>();

		foreach (var actor in world.Actors.Where(a => a.Owner == player && a.IsInWorld && !a.IsDead))
		{
			if (IsUnit(actor))
			{
				var unit = CreateOwnUnitInfo(actor);
				if (unit != null)
					ownUnits.Add(unit);
			}
		}

		return ownUnits;
	}

	List<ChallengeOwnStructure> GetOwnStructures()
	{
		var ownStructures = new List<ChallengeOwnStructure>();

		foreach (var actor in world.Actors.Where(a => a.Owner == player && a.IsInWorld && !a.IsDead))
		{
			if (IsStructure(actor))
			{
				var structure = CreateOwnStructureInfo(actor);
				if (structure != null)
					ownStructures.Add(structure);
			}
		}

		return ownStructures;
	}

	List<ChallengeVisibleEnemy> GetVisibleEnemies()
	{
		var visibleEnemies = new List<ChallengeVisibleEnemy>();

		foreach (var actor in world.Actors.Where(a => a.IsInWorld && !a.IsDead))
		{
			// Only consider enemy actors that are visible to this player
			if (actor.Owner != player && !actor.Owner.NonCombatant && actor.OccupiesSpace != null && player.Shroud.IsVisible(actor.CenterPosition))
			{
				var enemy = CreateVisibleEnemyInfo(actor);
				if (enemy != null)
					visibleEnemies.Add(enemy);
			}
		}

		return visibleEnemies;
	}

	List<ChallengeVisibleNeutralActor> GetNeutralActors()
	{
		var neutralActors = new List<ChallengeVisibleNeutralActor>();

		foreach (var actor in world.Actors.Where(a => a.IsInWorld && !a.IsDead))
		{
			// Only consider neutral actors that are visible to this player
			if (actor.Owner.NonCombatant && actor.OccupiesSpace != null && player.Shroud.IsVisible(actor.CenterPosition))
			{
				var neutral = CreateVisibleNeutralActorInfo(actor);
				if (neutral != null)
					neutralActors.Add(neutral);
			}
		}

		return neutralActors;
	}

	List<ChallengeObservationEvent> GetRecentEvents()
	{
		var events = new List<ChallengeObservationEvent>();

		// For now, return empty list - this can be extended later
		// to track recent game events like attacks, construction, etc.

		return events;
	}

	bool IsUnit(Actor actor)
	{
		if (actor == null || actor.Info == null)
			return false;

		// Check if actor has mobile traits (indicating it's a unit)
		return actor.Info.HasTraitInfo<MobileInfo>() ||
		       actor.Info.HasTraitInfo<AttackBaseInfo>() ||
		       actor.Info.HasTraitInfo<HarvesterInfo>();
	}

	bool IsStructure(Actor actor)
	{
		if (actor == null || actor.Info == null)
			return false;

		// Check if actor has building traits
		return actor.Info.HasTraitInfo<BuildingInfo>() ||
		       actor.Info.HasTraitInfo<ProductionInfo>() ||
		       actor.Info.HasTraitInfo<PowerInfo>();
	}

	ChallengeOwnUnit CreateOwnUnitInfo(Actor actor)
	{
		if (actor == null || actor.Info == null)
			return null;

		var health = actor.TraitOrDefault<IHealth>();
		var mobile = actor.TraitOrDefault<Mobile>();

		return new ChallengeOwnUnit
		{
			Id = actor.ActorID.ToString(),
			UnitType = actor.Info.Name,
			X = actor.Location.X,
			Y = actor.Location.Y,
			HpRatio = health != null && health.MaxHP > 0 ? (float)health.HP / health.MaxHP : 0f,
			AmmoRatio = 1f, // TODO: Implement ammo tracking
			State = actor.IsIdle ? "idle" : "active",
			IsSelectedCapable = true, // TODO: Check if unit can be selected
			Group = "" // TODO: Implement unit grouping
		};
	}

	ChallengeOwnStructure CreateOwnStructureInfo(Actor actor)
	{
		if (actor == null || actor.Info == null)
			return null;

		var health = actor.TraitOrDefault<IHealth>();
		var building = actor.TraitOrDefault<Building>();
		var production = actor.TraitOrDefault<Production>();
		var powerInfo = actor.TraitOrDefault<OpenRA.Mods.Common.Traits.Power>();
		var powerManager = actor.Owner.PlayerActor.Trait<PowerManager>();

		return new ChallengeOwnStructure
		{
			Id = actor.ActorID.ToString(),
			StructureType = actor.Info.Name,
			X = actor.Location.X,
			Y = actor.Location.Y,
			HpRatio = health != null && health.MaxHP > 0 ? (float)health.HP / health.MaxHP : 0f,
			ProductionQueue = production != null ? new List<string>() : new List<string>(), // TODO: Get actual production queue
			IsPowered = powerManager != null && powerManager.PowerProvided > powerManager.PowerDrained
		};
	}

	ChallengeVisibleEnemy CreateVisibleEnemyInfo(Actor actor)
	{
		if (actor == null || actor.Info == null)
			return null;

		var health = actor.TraitOrDefault<IHealth>();
		var attack = actor.TraitOrDefault<AttackBase>();

		return new ChallengeVisibleEnemy
		{
			Id = actor.ActorID.ToString(),
			EnemyType = actor.Info.Name,
			X = actor.Location.X,
			Y = actor.Location.Y,
			HpRatio = health != null && health.MaxHP > 0 ? (float)health.HP / health.MaxHP : 0f,
			Threat = CalculateThreat(actor, attack)
		};
	}

	ChallengeVisibleNeutralActor CreateVisibleNeutralActorInfo(Actor actor)
	{
		if (actor == null || actor.Info == null)
			return null;

		var health = actor.TraitOrDefault<IHealth>();

		return new ChallengeVisibleNeutralActor
		{
			Id = actor.ActorID.ToString(),
			ActorType = actor.Info.Name,
			X = actor.Location.X,
			Y = actor.Location.Y
		};
	}

	float CalculateThreat(Actor actor, AttackBase attack)
	{
		// Simple threat calculation based on attack capabilities
		if (attack == null)
			return 0f;

		// TODO: Implement more sophisticated threat calculation
		// For now, use a simple value based on whether the actor can attack
		return 1f;
	}
}
