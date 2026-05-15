using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ADOFAI;
using Iridium.Config;

namespace Iridium.Patches
{
    public static class JsonPatches
    {
        [HarmonyPatch(typeof(LevelData), nameof(LevelData.Decode))]
        public static class ForceAngleDataPatch
        {
            public static void Prefix(Dictionary<string, object> dict)
            {
                if (dict is null) return;
                if (!Main.Settings.compatibility.forceAngleData) return;
                if (!dict.TryGetValue("pathData", out object val) || val is not string pathData) return;

                dict["angleData"] = scrLevelMaker.StringToAngleArray(pathData).Cast<object>().ToList();
                dict.Remove("pathData");
            }
        }

        [HarmonyPatch(typeof(LevelData), nameof(LevelData.Decode))]
        public static class LegacyBehaviorPatch
        {
            public static void Postfix(LevelData __instance)
            {
                var comp = Main.Settings.compatibility;

                if (comp.legacyFlashMode != LegacyBehaviorMode.Default)
                {
                    __instance.legacyFlash = comp.legacyFlashMode == LegacyBehaviorMode.AlwaysOn;
                }

                if (comp.legacyCamRelativeToMode != LegacyBehaviorMode.Default)
                {
                    __instance.legacyCamRelativeTo = comp.legacyCamRelativeToMode == LegacyBehaviorMode.AlwaysOn;
                }
            }
        }
    }
}
