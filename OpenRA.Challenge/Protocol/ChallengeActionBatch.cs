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
public sealed class ChallengeActionBatch
{
	[JsonPropertyName("type")]
	public string Type { get; init; } = ChallengeProtocolConstants.ActionsType;

	[JsonPropertyName("api_version")]
	public string ApiVersion { get; init; } = ChallengeProtocolConstants.ApiVersionV1;

	[JsonPropertyName("match_id")]
	public string MatchId { get; init; } = "";

	[JsonPropertyName("player_id")]
	public string PlayerId { get; init; } = "";

	[JsonPropertyName("decision_id")]
	public int DecisionId { get; init; }

	[JsonPropertyName("actions")]
	public List<ChallengeAction> Actions { get; init; } = [];

	public void Validate(int maxActionsPerDecision)
	{
		ChallengeProtocolValidation.RequireTypeAndVersion(Type, ApiVersion, ChallengeProtocolConstants.ActionsType);
		ChallengeProtocolValidation.RequireNonEmpty(nameof(MatchId), MatchId);
		ChallengeProtocolValidation.RequireNonEmpty(nameof(PlayerId), PlayerId);
		ChallengeProtocolValidation.RequireNonNegativeDecisionId(DecisionId);
		if (Actions == null)
			throw new ChallengeProtocolException("Missing required field: actions.");

		if (maxActionsPerDecision > 0 && Actions.Count > maxActionsPerDecision)
			throw new ChallengeProtocolException($"Actions batch too large: {Actions.Count} > {maxActionsPerDecision}.");
	}
}
