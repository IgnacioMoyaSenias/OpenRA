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
public sealed class ChallengeEnd
{
	[JsonPropertyName("type")]
	public string Type { get; init; } = ChallengeProtocolConstants.EndType;

	[JsonPropertyName("api_version")]
	public string ApiVersion { get; init; } = ChallengeProtocolConstants.ApiVersionV1;

	[JsonPropertyName("match_id")]
	public string MatchId { get; init; } = "";

	[JsonPropertyName("player_id")]
	public string PlayerId { get; init; } = "";

	[JsonPropertyName("result")]
	public string Result { get; init; } = "";

	[JsonPropertyName("winner")]
	public string Winner { get; init; }

	[JsonPropertyName("ticks")]
	public int Ticks { get; init; }

	[JsonPropertyName("reason")]
	public string Reason { get; init; } = "";

	[JsonPropertyName("replay_path")]
	public string ReplayPath { get; init; } = "";

	[JsonPropertyName("artifact_hash")]
	public string ArtifactHash { get; init; } = "";

	[JsonPropertyName("summary")]
	public ChallengeEndSummary Summary { get; init; } = new();

	public void Validate()
	{
		ChallengeProtocolValidation.RequireTypeAndVersion(Type, ApiVersion, ChallengeProtocolConstants.EndType);
		ChallengeProtocolValidation.RequireNonEmpty(nameof(MatchId), MatchId);
		ChallengeProtocolValidation.RequireNonEmpty(nameof(PlayerId), PlayerId);
	}
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ChallengeEndSummary
{
	[JsonPropertyName("invalid_actions")]
	public int InvalidActions { get; init; }

	[JsonPropertyName("timeouts")]
	public int Timeouts { get; init; }

	[JsonPropertyName("own_units_lost")]
	public int OwnUnitsLost { get; init; }

	[JsonPropertyName("enemy_units_destroyed")]
	public int EnemyUnitsDestroyed { get; init; }
}
