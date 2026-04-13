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
using System.Text.Json.Serialization;

namespace OpenRA.Mods.Challenge.Protocol;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ChallengeAction
{
	[JsonPropertyName("kind")]
	public string Kind { get; init; } = "";

	[JsonPropertyName("producer_id")]
	public string ProducerId { get; init; }

	[JsonPropertyName("unit_type")]
	public string UnitType { get; init; }

	[JsonPropertyName("unit_ids")]
	public List<string> UnitIds { get; init; } = [];

	[JsonPropertyName("target")]
	public ChallengePoint Target { get; init; }

	[JsonPropertyName("target_actor_id")]
	public string TargetActorId { get; init; }

	[JsonPropertyName("target_pos")]
	public ChallengePoint TargetPos { get; init; }

	[JsonPropertyName("unit_id")]
	public string UnitId { get; init; }

	[JsonPropertyName("target_resource_id")]
	public string TargetResourceId { get; init; }

	[JsonPropertyName("factory_id")]
	public string FactoryId { get; init; }

	[JsonPropertyName("building_id")]
	public string BuildingId { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ChallengePoint
{
	[JsonPropertyName("x")]
	public int X { get; init; }

	[JsonPropertyName("y")]
	public int Y { get; init; }
}
