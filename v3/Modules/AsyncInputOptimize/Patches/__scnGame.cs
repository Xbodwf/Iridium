using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Iridium.Modules.AsyncInputOptimize.Patch
{
    [HarmonyPatch]
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
