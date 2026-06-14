using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using ADOFAI;
using DG.Tweening;
using UnityEngine;

namespace Iridium.Patches
{
    public static class BugfixPatches
    {
        [HarmonyPatch(typeof(scrController), "PortalTravelAction")]
        public static class PortalTravelFixPatch
        {
            private static FieldInfo? _f_transitioningLevel;
            private static System.Reflection.PropertyInfo? _p_isWipingToBlack;

            [HarmonyPrefix]
            public static bool Prefix(scrController __instance, Portal destination)
            {
                if (!Main.Settings.compatibility.portalTravelFix) return true;

                _f_transitioningLevel ??= AccessTools.Field(typeof(scrController), "transitioningLevel");

                if ((bool)_f_transitioningLevel.GetValue(__instance))
                    return false;

                var loader = ADOBase.loader;
                if (loader != null)
                {
                    _p_isWipingToBlack ??= AccessTools.Property(typeof(scrLoader), "isWipingToBlack");
                    if (_p_isWipingToBlack != null && (bool)_p_isWipingToBlack.GetValue(loader))
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// v2.10.0+: scrPlayer.marginTracker changed to a read-only property
        /// that directly returns scrMistakesManager.marginTrackers[playerID],
        /// so the sync from SetPlayerCount is now automatic. This patch is
        /// retained as a no-op stub for documentation clarity.
        /// </summary>
        // MarginTrackerSetPlayerCountFix removed in v2.10.0 — game handles sync natively.

        /// <summary>
        /// Forces a snap calibration each time a level starts playing. Without this,
        /// offsetTick starts at 0 and the per-frame slew (error/100) takes ~1.6s to
        /// converge to the true audio latency, causing incorrect timing at level start.
        /// </summary>
        [HarmonyPatch(typeof(scnGame), "Play")]
        public static class AsyncInputPlaySnapPatch
        {
            public static void Prefix()
            {
                if (AsyncInputManager.isActive)
                    AsyncInputUtils.UpdateOffsetTime(1L);
            }
        }

        /// <summary>
        /// v2.10.0: When scnGame.Play() runs in editor mode, it skips WaitForStartCo()
        /// and calls Awake_Rewind() + Start_Rewind() directly. Neither resets the
        /// mistakes manager, so hardestDifficulty retains a stale value (defaults to
        /// Lenient). SwitchToEditMode() calls mistakesManager.Reset() (which now uses
        /// the current marginTrackers thanks to MarginTrackerSetPlayerCountFix).
        /// This ensures Reset() is also called on the first Play from the editor.
        /// </summary>
        [HarmonyPatch(typeof(scnGame), "Play")]
        public static class EditorPlayResetMistakesPatch
        {
            public static void Prefix()
            {
                if (!ADOBase.isLevelEditor) return;
                scrController.instance?.mistakesManager.Reset();
            }
        }

        /// <summary>
        /// v2.10.0: CalculateSingleFloorAngleLength sets turnaround=true when
        /// |GetAngleMoved| <= 1e-6 OR >= 2π-ε. v2.9.8 only detected this when
        /// |GetAngleMoved - 2π| < 0.0001. The broader condition (1e-6 branch)
        /// catches wrap-around entry==exit floors that v2.9.8 did not, causing
        /// Pause events on those floors to incorrectly add an extra beat.
        /// This postfix re-checks turnaround using v2.9.8's exact condition.
        /// </summary>
        [HarmonyPatch(typeof(scrLevelMaker), "CalculateSingleFloorAngleLength")]
        public static class TurnaroundConditionFix
        {
            [HarmonyPostfix]
            public static void Postfix(scrFloor cf)
            {
                if (cf.turnaround)
                {
                    double angleMoved = scrMisc.GetAngleMoved(cf.entryangle, cf.exitangle, !cf.isCCW);
                    if (Math.Abs(angleMoved - 6.2831854820251465) >= 0.0001)
                        cf.turnaround = false;
                }
            }
        }


        /// <summary>
        /// Fixes vanilla v2.10.0 bug: non-coop ShowHitText doesn't forward missAngle
        /// to scrHitTextMesh.Show(), so non-Perfect judgments don't get the rotation
        /// animation. Uses _pendingMissAngle captured by HitTextManagerShowPatch.
        /// </summary>
        [HarmonyPatch(typeof(scrHitTextMesh), "Show")]
        public static class HitTextMeshShowRotationFixPatch
        {
            public static void Postfix(scrHitTextMesh __instance)
            {
                if (scrController.coopMode) return;
                if (__instance.hitMargin == HitMargin.Perfect) return;

                __instance.transform.DOLocalRotate(
                    new Vector3(0f, 0f, JudgeTextPatches._pendingMissAngle * 20f),
                    2f,
                    RotateMode.LocalAxisAdd
                );
            }
        }

        /// <summary>
        /// Coop mode: LockInput is global (scrController.responsive = false),
        /// which blocks ALL players' input when one player hits a pause beat.
        ///
        /// LockInput moved from scrController (≤3.1.1) to scrPlayer (≥3.1.2).
        /// This patch detects the target at runtime via reflection.
        ///
        /// Fix uses per-player pause tracking:
        ///   1) LockInput Prefix — coop mode skips global responsive=false
        ///   2) HandlePause Postfix — detects pause events, stores per-player state
        ///   3) scrPlayer.Hit Prefix — coop mode checks per-player pause state
        /// </summary>
        public static class CoopPauseLockFix
        {
            internal static readonly Dictionary<int, float> _playerPauseEndTimes = new();

            private static MethodBase? _patchedOriginal;
            private static MethodInfo? _prefixMethod;

            [HarmonyPrefix]
            public static bool Prefix()
            {
                return !scrController.coopMode;
            }

            /// <summary>
            /// Runtime detection: try scrPlayer.LockInput first (≥3.1.2),
            /// fall back to scrController.LockInput (≤3.1.1).
            /// Returns the patched original method, or null if neither exists.
            /// </summary>
            public static MethodBase? Apply(Harmony harmony)
            {
                _prefixMethod = SymbolExtensions.GetMethodInfo(() => Prefix());

                // ≥3.1.2: LockInput moved to scrPlayer
                var playerMethod = AccessTools.Method(typeof(scrPlayer), "LockInput");
                if (playerMethod != null)
                {
                    Main.Logger?.Log("CoopPauseLockFix: patching scrPlayer.LockInput (≥3.1.2)");
                    _patchedOriginal = harmony.Patch(playerMethod, prefix: new HarmonyMethod(_prefixMethod));
                    return _patchedOriginal;
                }

                // ≤3.1.1: LockInput is on scrController
                var controllerMethod = AccessTools.Method(typeof(scrController), "LockInput");
                if (controllerMethod != null)
                {
                    Main.Logger?.Log("CoopPauseLockFix: patching scrController.LockInput (≤3.1.1)");
                    _patchedOriginal = harmony.Patch(controllerMethod, prefix: new HarmonyMethod(_prefixMethod));
                    return _patchedOriginal;
                }

                Main.Logger?.Warning("CoopPauseLockFix: LockInput not found on scrPlayer or scrController, skipping");
                return null;
            }

            public static void Unapply(Harmony harmony)
            {
                if (_patchedOriginal != null && _prefixMethod != null)
                {
                    harmony.Unpatch(_patchedOriginal, _prefixMethod);
                    _patchedOriginal = null;
                    _prefixMethod = null;
                }
            }

            public static void SetPause(scrPlayer player, float lockTime)
            {
                if (player == null || lockTime <= 0f) return;
                float pitch = Mathf.Max(ADOBase.conductor.song.pitch, 0.001f);
                _playerPauseEndTimes[player.playerID] = Time.time + lockTime / pitch;
            }

            public static bool IsPaused(scrPlayer player)
            {
                if (player == null) return false;
                if (_playerPauseEndTimes.TryGetValue(player.playerID, out var endTime))
                {
                    if (Time.time >= endTime)
                    {
                        _playerPauseEndTimes.Remove(player.playerID);
                        return false;
                    }
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Detects when HandlePause enters the pause branch and stores
        /// per-player pause state for coop mode.
        /// Postfix-only: uses floor param to confirm this is a pause floor,
        /// then reads the private lockTime field.
        /// </summary>
        [HarmonyPatch(typeof(scrPlanet), nameof(scrPlanet.HandlePause))]
        public static class CoopPauseHandleLockFix
        {
            private static readonly AccessTools.FieldRef<scrPlanet, float> _getLockTime = AccessTools.FieldRefAccess<scrPlanet, float>("lockTime");

            [HarmonyPostfix]
            public static void Postfix(scrPlanet __instance, scrFloor floor)
            {
                if (!scrController.coopMode) return;
                if (floor == null || floor.freeroam || floor.extraBeats <= 0f) return;

                // HandlePause entered the pause branch; read the lockTime it set
                float lockTime = _getLockTime(__instance);
                if (lockTime <= 0f) return;

                // Same condition HandlePause uses before calling LockInput
                if (scrController.instance?.currentState != States.PlayerControl) return;

                CoopPauseLockFix.SetPause(__instance.player, lockTime);
            }
        }

        /// <summary>
        /// In coop mode, blocks scrPlayer.Hit() if this player is paused.
        /// Only blocks the pausing player; others play normally.
        /// </summary>
        [HarmonyPatch(typeof(scrPlayer), nameof(scrPlayer.Hit))]
        public static class CoopPlayerHitFix
        {
            [HarmonyPrefix]
            public static bool Prefix(scrPlayer __instance, ref bool __result)
            {
                if (!scrController.coopMode) return true;

                if (CoopPauseLockFix.IsPaused(__instance))
                {
                    __result = false;
                    return false; // Skip original Hit()
                }

                return true; // Let original Hit() run normally
            }
        }
    }
}
