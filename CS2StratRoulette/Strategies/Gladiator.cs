using CS2StratRoulette.Constants;
using CS2StratRoulette.Extensions;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CS2StratRoulette.Enums;

namespace CS2StratRoulette.Strategies
{
	[SuppressMessage("ReSharper", "UnusedType.Global")]
	public sealed class Gladiator : Strategy
	{
		public override string Name =>
			"Gladiator";

		public override string Description =>
			"Fuckin' Gladiator ya fuck!";

		public override StrategyFlags Flags =>
			StrategyFlags.AlwaysVisible;

		private GladiatorMapBounds bounds;

		private readonly List<CCSPlayerController> cts = new(Server.MaxPlayers / 2);
		private readonly List<CCSPlayerController> ts = new(Server.MaxPlayers / 2);

		private CCSPlayerController? ct;
		private CCSPlayerController? t;

		public override bool Start(ref CS2StratRoulettePlugin plugin)
		{
			if (!base.Start(ref plugin))
			{
				return false;
			}

			var map = Server.MapName;

			if (!GladiatorBounds.Maps.TryGetValue(map, out var mapBounds))
			{
				return false;
			}

			this.bounds = mapBounds;

			Server.ExecuteCommand(ConsoleCommands.BuyAllowNone);
			Server.ExecuteCommand(ConsoleCommands.BuyAllowGrenadesDisable);

			foreach (var (pos, angle) in this.bounds.Fences)
			{
				Gladiator.CreateFence(pos, angle);
			}

			var players = Utilities.GetPlayers();

			foreach (var controller in players)
			{
				if (!controller.TryGetPlayerPawn(out var pawn))
				{
					continue;
				}

				controller.EquipKnife();
				controller.RemoveWeapons();

				if (controller.Team is CsTeam.Terrorist)
				{
					pawn.RemoveC4();
				}
			}

			this.TeleportSpectators(players, this.bounds.Spectators.min, this.bounds.Spectators.max);
			this.PickGladiators();

			plugin.RegisterEventHandler<EventPlayerDeath>(this.OnPlayerDeath);

			return true;
		}

		public override bool Stop(ref CS2StratRoulettePlugin plugin)
		{
			if (!base.Stop(ref plugin))
			{
				return false;
			}

			Server.ExecuteCommand(ConsoleCommands.BuyAllowAll);
			Server.ExecuteCommand(ConsoleCommands.BuyAllowGrenadesEnable);

			plugin.DeregisterEventHandler<EventPlayerDeath>(this.OnPlayerDeath);

			return true;
		}

		private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo _)
		{
			if (!this.Running ||
				@event.Userid is null ||
				(@event.Userid.SteamID != this.ct?.SteamID &&
				 @event.Userid.SteamID != this.t?.SteamID))
			{
				return HookResult.Continue;
			}

			System.Console.WriteLine($"[Gladiator::OnPlayerDeath]: {@event.Userid.PlayerName} died");

			this.PickGladiators();

			return HookResult.Continue;
		}

		private void PickGladiators()
		{
			if (!this.Running)
			{
				return;
			}

			System.Console.WriteLine("[Gladiator::PickGladiators]: picking gladiators");

			this.ct = Gladiator.PickGladiator(this.ct, this.cts, this.bounds.Gladiators.ct);
			this.t = Gladiator.PickGladiator(this.t, this.ts, this.bounds.Gladiators.t);

			foreach (var controller in Utilities.GetPlayers())
			{
				string message;

				if (this.ct is null && this.t is null)
				{
					message = "It's a draw.";
				}
				else if (this.ct is null)
				{
					message = "The Ts have won!";
				}
				else if (this.t is null)
				{
					message = "The CTs have won!";
				}
				else
				{
					message = $"Next fight: {this.ct?.PlayerName} vs {this.t?.PlayerName}. Good luck!";
				}

				controller.PrintToCenter(message);
			}
		}

		private void TeleportSpectators(List<CCSPlayerController> players, Vector min, Vector max)
		{
			const float playerWidth = 32f;

			var i = 0;

			var playersX = (int)float.Abs(float.Floor((max.X - min.X) / playerWidth));
			var playersY = (int)float.Abs(float.Floor((max.Y - min.Y) / playerWidth));

			// @todo make vector
			var stepX = (min.X > max.X) ? -playerWidth : playerWidth;
			var stepY = (min.Y > max.Y) ? -playerWidth : playerWidth;

			for (var y = 0; y < playersY; y++)
			{
				for (var x = 0; x < playersX; x++)
				{
					if (i >= players.Count)
					{
						return;
					}

					var player = players[i++];

					if (player.Team is not (CsTeam.Terrorist or CsTeam.CounterTerrorist) ||
						!player.TryGetPlayerPawn(out var pawn))
					{
						continue;
					}

					if (player.Team is CsTeam.Terrorist)
					{
						this.ts.Add(player);
					}
					else
					{
						this.cts.Add(player);
					}

					pawn.Teleport(
						// @todo Z
						new(min.X + (stepX * x), min.Y + (stepY * y), min.Z),
						pawn.V_angle,
						Vector.Zero
					);
				}
			}
		}

		private static CCSPlayerController? PickGladiator(CCSPlayerController? controller,
														  List<CCSPlayerController> players,
														  Vector position)
		{
			if (controller is null || !controller.IsAlive())
			{
				controller = players.Find(static (e) => e.IsAlive());
			}

			if (controller is null || !controller.TryGetPlayerPawn(out var pawn))
			{
				System.Console.WriteLine("[Gladiator::PickGladiator]: controller null or invalid");

				return null;
			}

			System.Console.WriteLine($"[Gladiator::PickGladiator]: picked {controller.PlayerName}");

			Server.NextFrame(() =>
			{
				pawn.Teleport(position, pawn.V_angle, Vector.Zero);

				controller.GiveNamedItem(CsItem.KnifeT);
				controller.EquipKnife();
			});

			return controller;
		}

		private static void CreateFence(Vector position, QAngle angle)
		{
			var entity = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");

			if (entity is null || !entity.IsValid)
			{
				return;
			}

			Server.NextFrame(() =>
			{
				entity.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
				entity.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_NONE;
				entity.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_NONE;

				var collisionRulesChanged = new VirtualFunctionVoid<nint>(entity.Handle, 172);

				collisionRulesChanged.Invoke(entity.Handle);
			});

			Server.NextFrame(() =>
			{
				entity.DispatchSpawn();
				entity.SetModel(Models.Fence);
				entity.Teleport(position, angle, Vector.Zero);
			});
		}
	}
}
