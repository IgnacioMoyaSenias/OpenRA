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
using OpenRA.Traits;

namespace OpenRA.Mods.Challenge.Traits.Server
{
	public class AutoStartGame : IWorldLoaded
	{
		private readonly AutoStartGameInfo info;
		private int startDelayTicks;

		public AutoStartGame(Actor self, AutoStartGameInfo info)
		{
			this.info = info;
			startDelayTicks = info.StartDelay * 25; // 25 ticks per second
		}

		void IWorldLoaded.WorldLoaded(OpenRA.World world, OpenRA.Graphics.WorldRenderer renderer)
		{
			// Only auto-start in Challenge mode
			if (!Game.Settings.Challenge.Enabled || !info.Enabled)
				return;

			Console.WriteLine($"[Challenge] Auto-start feature enabled. Delay: {info.StartDelay} seconds");
			Console.WriteLine("[Challenge] Note: Manual game start required for now.");
			Console.WriteLine("[Challenge] Use the client to connect and start the game with Challenge bots.");
		}
	}
}
