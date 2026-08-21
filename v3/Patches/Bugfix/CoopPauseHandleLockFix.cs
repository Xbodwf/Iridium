using Iridium.Config;
using HarmonyLib;
using ADOFAI;

namespace Iridium.Patches.Bugfix
{
	[HarmonyPatch(typeof(scrPlanet), nameof(scrPlanet.HandlePause))]
	[IriPatch(Path = "bugfix/coopPause", Pre = typeof(CompatibilitySettings), Condition = "fixCoopPauseLock")]
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
}
