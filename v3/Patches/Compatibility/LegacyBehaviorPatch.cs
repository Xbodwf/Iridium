using HarmonyLib;
using ADOFAI;
using Iridium.Config;

namespace Iridium.Patches.Compatibility
{
	[IriPatch(Path = "compatibility/legacyBehavior", Pre = typeof(CompatibilitySettings), Condition = "legacyFlashMode" + "," + "legacyCamRelativeToMode")]
	[HarmonyPatch(typeof(LevelData), nameof(LevelData.Decode))]
	public static class LegacyBehaviorPatch
	{
		public static void Postfix(LevelData __instance)
		{
			var comp = Main.Settings.compatibility;

			if (comp.legacyFlashMode != LegacyBehaviorMode.Default)
				__instance.legacyFlash = comp.legacyFlashMode == LegacyBehaviorMode.AlwaysOn;

			if (comp.legacyCamRelativeToMode != LegacyBehaviorMode.Default)
				__instance.legacyCamRelativeTo = comp.legacyCamRelativeToMode == LegacyBehaviorMode.AlwaysOn;
		}
	}
}
