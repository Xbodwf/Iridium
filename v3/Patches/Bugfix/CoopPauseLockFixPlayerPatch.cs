using Iridium.Config;
using HarmonyLib;
using ADOFAI;

namespace Iridium.Patches.Bugfix
{
	[HarmonyPatch(typeof(scrPlayer), "LockInput")]
	[IriPatch(Path = "bugfix/coopPause", Pre = typeof(CompatibilitySettings), Condition = "fixCoopPauseLock", RequireMethod = "scrPlayer.LockInput")]
	public static class CoopPauseLockFixPlayerPatch
	{
		[HarmonyPrefix]
		public static bool Prefix() => !scrController.coopMode;
	}
}
