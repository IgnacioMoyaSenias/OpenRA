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

using System.Text.Json.Serialization;

namespace OpenRA.Mods.Challenge.Protocol;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ChallengeInit
{
	[JsonPropertyName("type")]
	public string Type { get; init; } = ChallengeProtocolConstants.InitType;

	[JsonPropertyName("api_version")]
	public string ApiVersion { get; init; } = ChallengeProtocolConstants.ApiVersionV1;

	[JsonPropertyName("match_id")]
	public string MatchId { get; init; } = "";

	[JsonPropertyName("player_id")]
	public string PlayerId { get; init; } = "";

	[JsonPropertyName("player_name")]
	public string PlayerName { get; init; } = "";

	[JsonPropertyName("map")]
	public ChallengeInitMapInfo Map { get; init; } = new();

	[JsonPropertyName("mod")]
	public ChallengeInitModInfo Mod { get; init; } = new();

	[JsonPropertyName("faction")]
	public string Faction { get; init; } = "";

	[JsonPropertyName("side")]
	public string Side { get; init; } = "";

	[JsonPropertyName("seed")]
	public ulong Seed { get; init; }

	[JsonPropertyName("decision_interval_ticks")]
	public int DecisionIntervalTicks { get; init; }

	[JsonPropertyName("response_deadline_ticks")]
	public int ResponseDeadlineTicks { get; init; }

	[JsonPropertyName("max_actions_per_decision")]
	public int MaxActionsPerDecision { get; init; }

	[JsonPropertyName("capabilities")]
	public ChallengeCapabilities Capabilities { get; init; } = new();

	[JsonPropertyName("visibility_mode")]
	public string VisibilityMode { get; init; } = "strict-fow";

	[JsonPropertyName("time_budget_ms_hint")]
	public int TimeBudgetMsHint { get; init; } = 50;

	public void Validate()
	{
		ChallengeProtocolValidation.RequireTypeAndVersion(Type, ApiVersion, ChallengeProtocolConstants.InitType);
		ChallengeProtocolValidation.RequireNonEmpty(nameof(MatchId), MatchId);
		ChallengeProtocolValidation.RequireNonEmpty(nameof(PlayerId), PlayerId);
		if (Map == null)
			throw new ChallengeProtocolException("Missing required field: map.");

		ChallengeProtocolValidation.RequireNonEmpty("map.id", Map.Id);
		ChallengeProtocolValidation.RequirePositive(nameof(DecisionIntervalTicks), DecisionIntervalTicks);
		ChallengeProtocolValidation.RequirePositive(nameof(ResponseDeadlineTicks), ResponseDeadlineTicks);
		ChallengeProtocolValidation.RequirePositive(nameof(MaxActionsPerDecision), MaxActionsPerDecision);
	}
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ChallengeInitMapInfo
{
	[JsonPropertyName("id")]
	public string Id { get; init; } = "";

	[JsonPropertyName("title")]
	public string Title { get; init; } = "";

	[JsonPropertyName("width")]
	public int Width { get; init; }

	[JsonPropertyName("height")]
	public int Height { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ChallengeInitModInfo
{
	[JsonPropertyName("id")]
	public string Id { get; init; } = "";

	[JsonPropertyName("version")]
	public string Version { get; init; } = "";

	[JsonPropertyName("engine_version")]
	public string EngineVersion { get; init; } = "";
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ChallengeCapabilities
{
	[JsonPropertyName("allow_build")]
	public bool AllowBuild { get; init; }

	[JsonPropertyName("allow_sell")]
	public bool AllowSell { get; init; }

	[JsonPropertyName("allow_research")]
	public bool AllowResearch { get; init; }

	[JsonPropertyName("allow_support_powers")]
	public bool AllowSupportPowers { get; init; }
}
