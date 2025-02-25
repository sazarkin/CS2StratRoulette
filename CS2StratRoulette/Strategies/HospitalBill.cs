using CS2StratRoulette.Extensions;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using System.Diagnostics.CodeAnalysis;
using CS2StratRoulette.Enums;

namespace CS2StratRoulette.Strategies
{
	[SuppressMessage("ReSharper", "UnusedType.Global")]
	public sealed class HospitalBill : Strategy
	{
		public override string Name =>
			"Hospital bills";

		public override string Description =>
			"When you die all other players in your team will lose $500 to pay for your hospital bills.";

		public override StrategyFlags Flags =>
			StrategyFlags.AlwaysVisible;

		public override bool Start(ref CS2StratRoulettePlugin plugin)
		{
			if (!base.Start(ref plugin))
			{
				return false;
			}

			plugin.RegisterEventHandler<EventPlayerDeath>(this.OnPlayerDeath);

			return true;
		}

		public override bool Stop(ref CS2StratRoulettePlugin plugin)
		{
			if (!base.Stop(ref plugin))
			{
				return false;
			}

			plugin.DeregisterEventHandler<EventPlayerDeath>(this.OnPlayerDeath);

			return true;
		}

		private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo _)
		{
			if (!this.Running)
			{
				return HookResult.Continue;
			}

			if (!@event.Userid.TryGetPlayerController(out var target))
			{
				return HookResult.Continue;
			}

			var team = target.Team;

			for (var slot = 0; slot < Server.MaxPlayers; slot++)
			{
				var controller = Utilities.GetPlayerFromSlot(slot);

				if (controller is null ||
					!controller.TryGetPlayerPawn(out var pawn) ||
					controller.Team != team ||
					controller.UserId == target.UserId)
				{
					continue;
				}

				var moneyServices = controller.InGameMoneyServices;

				if (moneyServices is null)
				{
					continue;
				}

				moneyServices.Account -= 500;

				Utilities.SetStateChanged(pawn, "CCSPlayerController", "m_pInGameMoneyServices");
			}

			return HookResult.Continue;
		}
	}
}
