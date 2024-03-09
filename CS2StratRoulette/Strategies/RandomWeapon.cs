using CS2StratRoulette.Enums;
using CS2StratRoulette.Extensions;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API;
using System.Diagnostics.CodeAnalysis;

namespace CS2StratRoulette.Strategies
{
	[SuppressMessage("ReSharper", "UnusedType.Global")]
	public sealed class RandomWeapon : Strategy
	{
		private const int PistolMin = (int)CsItem.Deagle;
		private const int PistolMax = (int)CsItem.Revolver;

		private const int MidMin = (int)CsItem.Mac10;
		private const int MidMax = (int)CsItem.Negev;

		private const int RifleMin = (int)CsItem.AK47;
		private const int RifleMax = (int)CsItem.G3SG1;

		public override string Name =>
			"Random weapon";

		public override string Description =>
			"I hope you like your new weapon :)";

		private readonly System.Random random = new();

		public override bool Start(ref CS2StratRoulettePlugin plugin)
		{
			if (!base.Start(ref plugin))
			{
				return false;
			}

			Server.ExecuteCommand($"mp_buy_allow_guns {BuyAllow.None.Str()}");
			Server.ExecuteCommand("mp_buy_allow_grenades 0");
			Server.ExecuteCommand("mp_weapons_allow_zeus 0");

			foreach (var player in Utilities.GetPlayers())
			{
				if (!player.TryGetPlayerController(out var controller))
				{
					continue;
				}

				if (!controller.TryGetPlayerPawn(out var pawn) || pawn.WeaponServices is null)
				{
					continue;
				}

				pawn.RemoveWeaponsByType(
					true,
					CSWeaponType.WEAPONTYPE_KNIFE,
					CSWeaponType.WEAPONTYPE_C4,
					CSWeaponType.WEAPONTYPE_EQUIPMENT
				);

				var item = this.random.Next(3) switch
				{
					0 => (CsItem)this.random.Next(RandomWeapon.PistolMin, RandomWeapon.PistolMax),
					1 => (CsItem)this.random.Next(RandomWeapon.MidMin, RandomWeapon.MidMax),
					_ => (CsItem)this.random.Next(RandomWeapon.RifleMin, RandomWeapon.RifleMax),
				};

				player.GiveNamedItem(item);
			}

			return true;
		}

		public override bool Stop(ref CS2StratRoulettePlugin plugin)
		{
			if (!base.Stop(ref plugin))
			{
				return false;
			}

			Server.ExecuteCommand($"mp_buy_allow_guns {BuyAllow.All.Str()}");
			Server.ExecuteCommand("mp_buy_allow_grenades 1");
			Server.ExecuteCommand("mp_weapons_allow_zeus 1");

			return true;
		}
	}
}
