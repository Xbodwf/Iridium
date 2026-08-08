using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Iridium.Modules.AsyncInputOptimize.Patch
{
    [HarmonyPatch]
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
