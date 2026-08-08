using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Iridium.Modules.AsyncInputOptimize.Patch
{
    [HarmonyPatch]
    public static class __scrConductor
    {
        [HarmonyPatch(typeof(scrConductor), "Start")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler_Start(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PatchMidLayer), nameof(PatchMidLayer.StartOrPlay)));
            foreach (CodeInstruction ci in instructions)
            {
                yield return SafeDSPTime.ReplaceDSPTime(ci);
            }
            yield break;
        }
        [HarmonyPatch(typeof(scrConductor), "Rewind")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler_Rewind(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PatchMidLayer), nameof(PatchMidLayer.StartOrPlay)));
            foreach (CodeInstruction ci in instructions)
            {
                yield return SafeDSPTime.ReplaceDSPTime(ci);
            }
            yield break;
        }
        [HarmonyPatch(typeof(scrConductor), "Update")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler_Update(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PatchMidLayer), nameof(PatchMidLayer.ConductorUpdate)));

            // R110: skip until Callvirt UpdateInput
            // R136+: skip until Stfld prev_unityDspTime
            bool skip = true;
            foreach (CodeInstruction ci in instructions)
            {
                if (GameVersion.IsR110)
                {
                    if (ci.opcode == OpCodes.Callvirt && (ci.operand as MethodInfo)?.Name == "UpdateInput")
                    {
                        skip = false;
                        continue;
                    }
                }
                else
                {
                    if (ci.opcode == OpCodes.Stfld && (ci.operand as FieldInfo)?.Name == "prev_unityDspTime")
                    {
                        skip = false;
                        continue;
                    }
                }
                if (skip)
                {
                    continue;
                }
                yield return SafeDSPTime.ReplaceDSPTime(ci);
            }
            yield break;
        }
    }
}
