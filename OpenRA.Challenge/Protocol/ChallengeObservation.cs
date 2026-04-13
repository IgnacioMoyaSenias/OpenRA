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
using System.Text.Json.Serialization;

namespace OpenRA.Mods.Challenge.Protocol;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ChallengeObservation
{
	[JsonPropertyName("type")]
	public string Type { get; init; } = ChallengeProtocolConstants.ObservationType;

	[JsonPropertyName("api_version")]
	public string ApiVersion { get; init; } = ChallengeProtocolConstants.ApiVersionV1;

	[JsonPropertyName("match_id")]
	public string MatchId { get; init; } = "";

	[JsonPropertyName("player_id")]
	public string PlayerId { get; init; } = "";

	[JsonPropertyName("decision_id")]
	public int DecisionId { get; init; }

	[JsonPropertyName("tick")]
	public int Tick { get; init; }

	[JsonPropertyName("resources")]
	public ChallengeResourceSnapshot Resources { get; init; } = new();

	[JsonPropertyName("scoreboard")]
	public ChallengeScoreboardSnapshot Scoreboard { get; init; } = new();

	[JsonPropertyName("map")]
	public ChallengeObservationMapInfo Map { get; init; } = new();

	[JsonPropertyName("own_units")]
	public List<ChallengeOwnUnit> OwnUnits { get; init; } = [];

	[JsonPropertyName("own_structures")]
	public List<ChallengeOwnStructure> OwnStructures { get; init; } = [];

	[JsonPropertyName("visible_enemies")]
	public List<ChallengeVisibleEnemy> VisibleEnemies { get; init; } = [];

	[JsonPropertyName("visible_neutral_actors")]
	public List<ChallengeVisibleNeutralActor> VisibleNeutralActors { get; init; } = [];

	[JsonPropertyName("resource_nodes")]
	public List<ChallengeResourceNode> ResourceNodes { get; init; } = [];

	[JsonPropertyName("events")]
	public List<ChallengeObservationEvent> Events { get; init; } = [];

	[JsonPropertyName("visibility_mask_ref")]
	public string VisibilityMaskRef { get; init; }

	public void Validate()
	{
		ChallengeProtocolValidation.RequireTypeAndVersion(Type, ApiVersion, ChallengeProtocolConstants.ObservationType);
		ChallengeProtocolValidation.RequireNonEmpty(nameof(MatchId), MatchId);
		ChallengeProtocolValidation.RequireNonEmpty(nameof(PlayerId), PlayerId);
		ChallengeProtocolValidation.RequireNonNegativeDecisionId(DecisionId);
	}
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ChallengeResourceSnapshot
{
	[JsonPropertyName("cash")]
	public int Cash { get; init; }

	[JsonPropertyName("power")]
	public int Power { get; init; }

	[JsonPropertyName("power_used")]
	public int PowerUsed { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ChallengeScoreboardSnapshot
{
	[JsonPropertyName("winner_if_finished")]
	public string WinnerIfFinished { get; init; }

	[JsonPropertyName("elapsed_ticks")]
	public int ElapsedTicks { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ChallengeObservationMapInfo
{
	[JsonPropertyName("width")]
	public int Width { get; init; }

	[JsonPropertyName("height")]
	public int Height { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ChallengeOwnUnit
{
	[JsonPropertyName("id")]
	public string Id { get; init; } = "";

	[JsonPropertyName("type")]
	public string UnitType { get; init; } = "";

	[JsonPropertyName("x")]
	public int X { get; init; }

	[JsonPropertyName("y")]
	public int Y { get; init; }

	[JsonPropertyName("hp_ratio")]
	public float HpRatio { get; init; }

	[JsonPropertyName("ammo_ratio")]
	public float AmmoRatio { get; init; }

	[JsonPropertyName("state")]
	public string State { get; init; } = "";

	[JsonPropertyName("is_selected_capable")]
	public bool IsSelectedCapable { get; init; }

	[JsonPropertyName("group")]
	public string Group { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ChallengeOwnStructure
{
	[JsonPropertyName("id")]
	public string Id { get; init; } = "";

	[JsonPropertyName("type")]
	public string StructureType { get; init; } = "";

	[JsonPropertyName("x")]
	public int X { get; init; }

	[JsonPropertyName("y")]
	public int Y { get; init; }

	[JsonPropertyName("hp_ratio")]
	public float HpRatio { get; init; }

	[JsonPropertyName("production_queue")]
	public List<string> ProductionQueue { get; init; } = [];

	[JsonPropertyName("is_powered")]
	public bool IsPowered { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ChallengeVisibleEnemy
{
	[JsonPropertyName("id")]
	public string Id { get; init; } = "";

	[JsonPropertyName("type")]
	public string EnemyType { get; init; } = "";

	[JsonPropertyName("x")]
	public int X { get; init; }

	[JsonPropertyName("y")]
	public int Y { get; init; }

	[JsonPropertyName("hp_ratio")]
	public float HpRatio { get; init; }

	[JsonPropertyName("threat")]
	public float Threat { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ChallengeVisibleNeutralActor
{
	[JsonPropertyName("id")]
	public string Id { get; init; } = "";

	[JsonPropertyName("type")]
	public string ActorType { get; init; } = "";

	[JsonPropertyName("x")]
	public int X { get; init; }

	[JsonPropertyName("y")]
	public int Y { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ChallengeResourceNode
{
	[JsonPropertyName("id")]
	public string Id { get; init; } = "";

	[JsonPropertyName("type")]
	public string ResourceType { get; init; } = "";

	[JsonPropertyName("x")]
	public int X { get; init; }

	[JsonPropertyName("y")]
	public int Y { get; init; }

	[JsonPropertyName("estimated_value")]
	public int EstimatedValue { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ChallengeObservationEvent
{
	[JsonPropertyName("kind")]
	public string Kind { get; init; } = "";

	[JsonPropertyName("actor_id")]
	public string ActorId { get; init; } = "";

	[JsonPropertyName("source_actor_id")]
	public string SourceActorId { get; init; }

	[JsonPropertyName("tick")]
	public int Tick { get; init; }
}
