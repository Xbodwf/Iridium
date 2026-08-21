using Iridium.Config;
using HarmonyLib;
using ADOFAI;

namespace Iridium.Patches.Bugfix
{
	[HarmonyPatch(typeof(scrPlayer), nameof(scrPlayer.Hit))]
	[IriPatch(Path = "bugfix/coopPause", Pre = typeof(CompatibilitySettings), Condition = "fixCoopPauseLock")]
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
