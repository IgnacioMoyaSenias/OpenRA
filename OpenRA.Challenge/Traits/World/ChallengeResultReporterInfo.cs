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

using OpenRA.Traits;

namespace OpenRA.Mods.Challenge.Traits.World
{
	[Desc("Automatically reports match results and metrics for Challenge games.")]
	public sealed class ChallengeResultReporterInfo : TraitInfo
	{
		[Desc("Directory where results will be saved.")]
		public readonly string ResultsDirectory = "challenge-results";

		[Desc("Whether to save detailed metrics in metrics.jsonl format.")]
		public readonly bool SaveMetrics = true;

		[Desc("Whether to save replay files.")]
		public readonly bool SaveReplay = true;

		[Desc("Interval in ticks for collecting metrics (0 = disabled).")]
		public readonly int MetricsInterval = 100;

		public override object Create(ActorInitializer init) { return new ChallengeResultReporter(init.Self, this); }
	}
}
