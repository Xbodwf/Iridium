using Iridium.Patches;
using Iridium.Config;
﻿using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Iridium;

namespace Iridium.Modules.AsyncInputOptimize.Patch
{
    [HarmonyPatch]
    [IriPatch(Path = "asyncInput", Pre = typeof(AsyncInputSettings), Condition = "enableAIO")]
    public static class __scrCountdown
    {
        [HarmonyPatch(typeof(scrCountdown), "Update")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler_Update(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            foreach (CodeInstruction ci in instructions)
            {
                yield return SafeDSPTime.ReplaceDSPTime(ci);
            }
            yield break;
        }
    }
}
