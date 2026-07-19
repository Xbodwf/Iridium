using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using ADOFAI;
using DG.Tweening;
using UnityEngine;

namespace Iridium.Patches
{
	public static class BugfixPatches
	{
		private const double TwoPi = 6.2831854820251465;
		private const double TurnaroundEpsilon = 0.0001;

		[HarmonyPatch(typeof(scrController), "PortalTravelAction")]
		public static class PortalTravelFixPatch
		{
			private static FieldInfo? _f_transitioningLevel;
			private static System.Reflection.PropertyInfo? _p_isWipingToBlack;

			[HarmonyPrefix]
			public static bool Prefix(scrController __instance, Portal destination)
			{
				if (!Main.Settings.compatibility.portalTravelFix) return true;

				_f_transitioningLevel ??= AccessTools.Field(typeof(scrController), "transitioningLevel");

				if ((bool)_f_transitioningLevel.GetValue(__instance))
					return false;

				var loader = ADOBase.loader;
				if (loader != null)
				{
					_p_isWipingToBlack ??= AccessTools.Property(typeof(scrLoader), "isWipingToBlack");
					if (_p_isWipingToBlack != null && (bool)_p_isWipingToBlack.GetValue(loader))
						return false;
				}

				return true;
			}
		}

		[HarmonyPatch(typeof(scnGame), "Play")]
		public static class AsyncInputPlaySnapPatch
		{
			[HarmonyPrefix]
			public static void Prefix()
			{
				if (AsyncInputManager.isActive)
					AsyncInputUtils.UpdateOffsetTime(1L);
			}
		}

		[HarmonyPatch(typeof(scnGame), "Play")]
		public static class EditorPlayResetMistakesPatch
		{
			[HarmonyPrefix]
			public static void Prefix()
			{
				if (!ADOBase.isLevelEditor) return;
				scrController.instance?.mistakesManager.Reset();
			}
		}

		[HarmonyPatch(typeof(scrLevelMaker), "CalculateSingleFloorAngleLength")]
		public static class TurnaroundConditionFix
		{
			[HarmonyPostfix]
			public static void Postfix(scrFloor cf)
			{
				if (cf.turnaround)
				{
					double angleMoved = scrMisc.GetAngleMoved(cf.entryangle, cf.exitangle, !cf.isCCW);
					if (Math.Abs(angleMoved - TwoPi) >= TurnaroundEpsilon)
						cf.turnaround = false;
				}
			}
		}

		public static class CoopPauseLockFix
		{
			internal static readonly Dictionary<int, float> _playerPauseEndTimes = new();

			public static void SetPause(scrPlayer player, float lockTime)
			{
				if (player == null || lockTime <= 0f) return;
				float pitch = Mathf.Max(ADOBase.conductor.song.pitch, 0.001f);
				_playerPauseEndTimes[player.playerID] = Time.time + lockTime / pitch;
			}

			public static bool IsPaused(scrPlayer player)
			{
				if (player == null) return false;
				if (_playerPauseEndTimes.TryGetValue(player.playerID, out var endTime))
				{
					if (Time.time >= endTime)
					{
						_playerPauseEndTimes.Remove(player.playerID);
						return false;
					}
					return true;
				}
				return false;
			}
		}

		[HarmonyPatch(typeof(scrPlayer), "LockInput")]
		public static class CoopPauseLockFixPlayerPatch
		{
			[HarmonyPrefix]
			public static bool Prefix() => !scrController.coopMode;
		}

		[HarmonyPatch(typeof(scrController), "LockInput")]
		public static class CoopPauseLockFixControllerPatch
		{
			[HarmonyPrefix]
			public static bool Prefix() => !scrController.coopMode;
		}

		[HarmonyPatch(typeof(scrPlanet), nameof(scrPlanet.HandlePause))]
		public static class CoopPauseHandleLockFix
		{
			private static readonly AccessTools.FieldRef<scrPlanet, float> _getLockTime = AccessTools.FieldRefAccess<scrPlanet, float>("lockTime");

			[HarmonyPostfix]
			public static void Postfix(scrPlanet __instance, scrFloor floor)
			{
				if (!scrController.coopMode) return;
				if (floor == null || floor.freeroam || floor.extraBeats <= 0f) return;

				float lockTime = _getLockTime(__instance);
				if (lockTime <= 0f) return;

				if (scrController.instance?.currentState != States.PlayerControl) return;

				CoopPauseLockFix.SetPause(__instance.player, lockTime);
			}
		}

		[HarmonyPatch(typeof(scrPlayer), nameof(scrPlayer.Hit))]
		public static class CoopPlayerHitFix
		{
			[HarmonyPrefix]
			public static bool Prefix(scrPlayer __instance, ref bool __result)
			{
				if (!scrController.coopMode) return true;

				if (CoopPauseLockFix.IsPaused(__instance))
				{
					__result = false;
					return false;
				}

				return true;
			}
		}
	}
}
