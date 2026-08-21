using Iridium.Config;
using HarmonyLib;
using ADOFAI;

namespace Iridium.Patches.Bugfix
{
	[HarmonyPatch(typeof(scrController), "LockInput")]
	[IriPatch(Path = "bugfix/coopPause", Pre = typeof(CompatibilitySettings), Condition = "fixCoopPauseLock", RequireMethod = "scrController.LockInput")]
	public static class CoopPauseLockFixControllerPatch
	{
		[HarmonyPrefix]
		public static bool Prefix() => !scrController.coopMode;
	}
}
