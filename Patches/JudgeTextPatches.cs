using System;
using System.Reflection;
using HarmonyLib;
using Iridium.Config;
using TMPro;
using UnityEngine;

namespace Iridium.Patches
{
    /// <summary>
    /// Judge Text Patches - Customizes judge text display with offset support
    /// Uses SuperStrictJudge's approach for accurate timing calculation
    /// </summary>
    public static class JudgeTextPatches
    {
        // Cached settings reference for performance
        private static JudgeTextSettings Settings => Main.Settings.judgeText;

        // NOTE: HitTextMeshInitPatch, HitTextMeshShowPatch, CalculateTimingFromAngle, GetOffsetText
        // moved to StdPatchMethod classes in MethodPatches.cs

        /// <summary>
        /// Patch for scrController.Awake_Rewind - Reset state (if any) on rewind
        /// </summary>
        [HarmonyPatch(typeof(scrController), "Awake_Rewind")]
        public static class ResetTimingOnRewindPatch
        {
            public static void Postfix()
            {
                // No global state anymore, but keeping for future use or consistency
            }
        }
    }
}
