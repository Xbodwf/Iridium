using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using GDMiniJSON;
using HarmonyLib;
using ADOFAI;
using Iridium.Config;

namespace Iridium.Patches
{
    public static class JsonPatches
    {
        private static readonly MethodInfo _deserializePartially = typeof(Json).GetMethod(nameof(Json.DeserializePartially), new[] { typeof(string), typeof(string) });

        /// <summary>
        /// LevelDataCLS.LoadLevel 只取 settings，跳过 actions 数组解析。
        /// </summary>
        [HarmonyPatch(typeof(LevelDataCLS), nameof(LevelDataCLS.LoadLevel))]
        public static class PatchLevelDataCLSLoadLevel
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var inst in instructions)
                {
                    if (inst.opcode == OpCodes.Call && inst.operand is MethodInfo method &&
                        method.Name == nameof(Json.Deserialize) && method.DeclaringType == typeof(Json))
                    {
                        yield return new CodeInstruction(OpCodes.Ldstr, "actions");
                        yield return new CodeInstruction(OpCodes.Call, _deserializePartially);
                    }
                    else
                    {
                        yield return inst;
                    }
                }
            }
        }

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
