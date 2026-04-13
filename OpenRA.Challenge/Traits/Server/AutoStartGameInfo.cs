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

namespace OpenRA.Mods.Challenge.Traits.Server
{
	[Desc("Automatically starts a game with Challenge bots when in Challenge mode.")]
	public sealed class AutoStartGameInfo : TraitInfo
	{
		[Desc("Delay in seconds before auto-starting the game.")]
		public readonly int StartDelay = 5;

		[Desc("Map to use for auto-started games. Empty = random map.")]
		public readonly string Map = "";

		[Desc("Number of Challenge bots to add.")]
		public readonly int BotCount = 1;

		[Desc("Whether to auto-start games in Challenge mode.")]
		public readonly bool Enabled = true;

		public override object Create(ActorInitializer init) { return new AutoStartGame(init.Self, this); }
	}
}
