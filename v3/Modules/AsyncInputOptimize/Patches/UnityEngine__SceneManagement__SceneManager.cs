using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Iridium.Modules.AsyncInputOptimize.Patch
{
    [HarmonyPatch]
    public static class UnityEngine__SceneManagement__SceneManager
    {
        [HarmonyPatch(typeof(UnityEngine.SceneManagement.SceneManager), "LoadSceneAsyncNameIndexInternal")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler_LoadSceneAsyncNameIndexInternal(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction ci in instructions)
            {
                yield return ci;
            }
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PatchMidLayer), nameof(PatchMidLayer.Reset)));
            yield break;
        }
    }
}
