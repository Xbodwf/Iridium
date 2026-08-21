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
    public static class __scnGame
    {
        [HarmonyPatch(typeof(scnGame), "Play")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler_Play(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PatchMidLayer), nameof(PatchMidLayer.StartOrPlay)));
            foreach (CodeInstruction ci in instructions)
            {
                yield return SafeDSPTime.ReplaceDSPTime(ci);
            }
            yield break;
        }
    }
}
