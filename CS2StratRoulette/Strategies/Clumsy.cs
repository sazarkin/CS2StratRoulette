﻿using CS2StratRoulette.Extensions;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API;
using System.Diagnostics.CodeAnalysis;

namespace CS2StratRoulette.Strategies
{
	[SuppressMessage("ReSharper", "UnusedType.Global")]
	public sealed class Clumsy : Strategy
	{
		private const float Interval = 5f;

		private static readonly System.Random Random = new();

		public override string Name =>
			"Clumsy";

		public override string Description =>
			"First day on the job";

		private Timer? timer;

		public override bool Start(ref CS2StratRoulettePlugin plugin)
		{
			if (!base.Start(ref plugin))
			{
				return false;
			}

			this.timer = new Timer(Clumsy.Interval, this.OnInterval, TimerFlags.REPEAT);

			return true;
		}

		public override bool Stop(ref CS2StratRoulettePlugin plugin)
		{
			if (!base.Stop(ref plugin))
			{
				return false;
			}

			this.timer?.Kill();

			return true;
		}

		private void OnInterval()
		{
			if (!this.Running)
			{
				return;
			}

			foreach (var controller in Utilities.GetPlayers())
			{
				if (Clumsy.Random.Next(10) < 6)
				{
					continue;
				}

				if (!controller.TryGetPlayerPawn(out var pawn) || pawn.WeaponServices is null)
				{
					continue;
				}

				if (!pawn.WeaponServices.ActiveWeapon.TryGetValue(out var weapon) || !weapon.TryGetData(out var data))
				{
					continue;
				}

				// @todo
				if (data.Slot != 3)
				{
					controller.DropActiveWeapon();
				}
			}
		}
	}
}
