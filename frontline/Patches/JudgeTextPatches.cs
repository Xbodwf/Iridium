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

        /// <summary>
        /// Calculate timing offset from angular offset in radians.
        /// angularOffset = targetExitAngle - actualAngle (as passed to scrHitTextMesh.Show)
        /// 
        /// Sign convention (User requested):
        /// 提前 (Early hit) = +
        /// 错过 (Late hit) = -
        /// </summary>
        private static double CalculateTimingFromAngle(float angularOffset)
        {
            var controller = scrController.instance;
            var conductor = scrConductor.instance;
            if (controller == null || conductor == null) return 0;

            double bpm = conductor.bpm;
            double speed = controller.d_speed;
            double pitch = conductor.song.pitch;

            // Standard Timing (Early = Negative, Late = Positive)
            // angularOffset is (target - actual). 
            // If isCW (Clockwise, angle decreasing): Early means actual > target, so angularOffset < 0.
            // If !isCW (Counter-Clockwise, angle increasing): Early means actual < target, so angularOffset > 0.
            double standardTiming = angularOffset * (controller.playerOne.planetarySystem.isCW ? 1.0 : -1.0) * 60000.0 / (Math.PI * bpm * speed * pitch);
            
            // User requested: 提前 (Early) = +, 错过 (Late) = -
            // This is the exact inverse of standard timing.
            return -standardTiming;
        }

        /// <summary>
        /// Get display text for offset mode
        /// </summary>
        private static string GetOffsetText(double timing)
        {
            if (double.IsNaN(timing) || double.IsInfinity(timing))
                return "0ms";
            
            // Display as integer (F0) as per user example "5ms"
            // Use Math.Round to ensure 0.5ms becomes 1ms
            long ms = (long)Math.Round(timing);
            return $"{(ms >= 0 ? "+" : "-")}{Math.Abs(ms)}ms";
        }

        /// <summary>
        /// Patch for scrHitTextMesh.Init - Handles custom judge text mode (not offset mode)
        /// </summary>
        [HarmonyPatch(typeof(scrHitTextMesh), "Init")]
        public static class HitTextMeshInitPatch
        {
            public static void Postfix(scrHitTextMesh __instance, HitMargin hitMargin, TextMeshPro ___text)
            {
                if (!Settings.enableJudgeTextCustomization) return;
                if (Settings.showAsOffset) return;

                ___text.text = Settings.GetTextForHitMargin((int)hitMargin);
            }
        }

        // Static field to pass missAngle from ShowHitText to Show
        private static float _pendingMissAngle = 0f;

        /// <summary>
        /// Patch for scrHitTextManager.ShowHitText - Captures missAngle before Show is called
        /// </summary>
        [HarmonyPatch(typeof(scrHitTextManager), "ShowHitText")]
        public static class HitTextManagerShowPatch
        {
            public static void Prefix(float missAngle)
            {
                _pendingMissAngle = missAngle;
            }
        }

        /// <summary>
        /// Patch for scrHitTextMesh.Show - Modifies text before display animations start
        /// Uses _pendingMissAngle captured from ShowHitText to calculate timing offset.
        /// This runs as Prefix so the text is already modified when Show makes it visible,
        /// preventing the official judgment text from flashing briefly.
        /// </summary>
        [HarmonyPatch(typeof(scrHitTextMesh), "Show")]
        public static class HitTextMeshShowPatch
        {
            public static void Prefix(scrHitTextMesh __instance, TextMeshPro ___text)
            {
                if (!Settings.enableJudgeTextCustomization || !Settings.showAsOffset) return;

                if (___text != null)
                {
                    double timing = CalculateTimingFromAngle(_pendingMissAngle);
                    ___text.text = GetOffsetText(timing);
                }
            }
        }

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
