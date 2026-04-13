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

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Challenge.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Configuration for an external-agent-driven bot module.")]
	public sealed class ExternalAgentBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Interval (in ticks) between decision requests to the external agent.")]
		public readonly int DecisionInterval = 5;

		[Desc("Maximum number of actions the external agent can emit per decision.")]
		public readonly int MaxActionsPerDecision = 8;

		[Desc("Maximum response time in ticks before a decision is considered timed out.")]
		public readonly int ResponseDeadlineTicks = 20;

		[Desc("Fog of war mode exposed to the external agent.")]
		public readonly string FogOfWarMode = "Partial";

		[Desc("Whether the external agent may issue build actions.")]
		public readonly bool AllowBuild = true;

		[Desc("Whether the external agent may issue sell actions.")]
		public readonly bool AllowSell = true;

		[Desc("Whether the external agent may issue research actions.")]
		public readonly bool AllowResearch = true;

		[Desc("Whether the external agent may issue support power actions.")]
		public readonly bool AllowSupportPowers = true;

		public override object Create(ActorInitializer init) { return new ExternalAgentBotModule(init.Self, this); }
	}
}
