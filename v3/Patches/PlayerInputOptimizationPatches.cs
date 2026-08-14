using HarmonyLib;
using System;
using UnityEngine;

namespace Iridium.Patches
{
    /// <summary>
    /// Eliminates per-frame heap allocations in the player input hot path.
    ///
    /// Vanilla scrPlayer.Simulated_PlayerControl_Update (called every frame per
    /// player) wraps hold-check callbacks in AsyncInputUtils.WhileFloorNotChange
    /// using captured lambdas:
    ///
    ///     AsyncInputUtils.WhileFloorNotChange(this, delegate { CheckPostHoldFail(targetTick); });
    ///     AsyncInputUtils.WhileFloorNotChange(this, delegate { OttoHoldHit(targetTick); });
    ///     ... (5 total)
    ///
    /// Each lambda captures `this` + the `targetTick` local and is allocated
    /// every single frame — ~5 allocations/frame/player (10 in 2P coop).
    ///
    /// This patch replaces the method with an equivalent implementation that
    /// invokes the hold-check callbacks via compiled delegates (created once
    /// via AccessTools.MethodDelegate) instead of per-frame lambdas, and
    /// preserves the loop-until-floor-stable semantics of WhileFloorNotChange.
    /// Private fields are accessed via cached FieldRefs. Per-frame allocations
    /// drop to zero.
    /// </summary>
    public static class PlayerInputOptimizationPatches
    {
        // Compiled delegates to private scrPlayer methods — created once, reused.
        private static Action<scrPlayer, ulong?>? _checkPostHoldFail;
        private static Action<scrPlayer, ulong?>? _ottoHoldHit;
        private static Action<scrPlayer, ulong?>? _hitAutoFloors;
        private static Action<scrPlayer, ulong?>? _updateHoldBehavior;
        private static Action<scrPlayer, ulong?>? _hitHoldFloorsIfStartedAtHold;
        private static Action<scrPlayer, ulong?>? _checkPreHoldFail;
        private static Action<scrPlayer, ulong?>? _updateHoldKeys;

        // Cached FieldRefs to private scrPlayer fields.
        private static AccessTools.FieldRef<scrPlayer, bool>? _nextTileIsHoldCachedRef;
        private static AccessTools.FieldRef<scrPlayer, bool>? _validInputWasReleasedThisFrameRef;
        private static AccessTools.FieldRef<scrPlayer, Vector2>? _cachedCamyToPosRef;

        private static Action<scrPlayer, ulong?> GetDelegate(ref Action<scrPlayer, ulong?>? cache, string methodName)
        {
            if (cache != null) return cache;
            var method = AccessTools.Method(typeof(scrPlayer), methodName);
            cache = AccessTools.MethodDelegate<Action<scrPlayer, ulong?>>(method, null);
            return cache;
        }

        [HarmonyPatch(typeof(scrPlayer), "Simulated_PlayerControl_Update")]
        public static class SimulatedPlayerControlUpdatePatch
        {
            [HarmonyPrefix]
            public static bool Prefix(scrPlayer __instance, ulong? targetTick)
            {
                // Opt-in optimization — when disabled, fall through to the original.
                if (!Main.Settings.optimizer.optimizePlayerInputAllocations)
                {
                    return true;
                }

                if (!__instance.alive || ADOBase.controller.paused || __instance.currFloor == null || ADOBase.controller.isCutscene)
                {
                    return false;
                }

                _nextTileIsHoldCachedRef ??= AccessTools.FieldRefAccess<scrPlayer, bool>("__nextTileIsHoldCached");
                _validInputWasReleasedThisFrameRef ??= AccessTools.FieldRefAccess<scrPlayer, bool>("validInputWasReleasedThisFrame");
                _cachedCamyToPosRef ??= AccessTools.FieldRefAccess<scrPlayer, Vector2>("cachedCamyToPos");

                _nextTileIsHoldCachedRef(__instance) = false;
                _validInputWasReleasedThisFrameRef(__instance) = __instance.ValidInputWasReleased();
                _cachedCamyToPosRef(__instance) = ADOBase.controller.camy.topos;

                if ((bool)__instance.currFloor.nextfloor)
                {
                    scrFloor nextfloor = __instance.currFloor.nextfloor;
                    while (nextfloor.midSpin && (bool)nextfloor.nextfloor)
                    {
                        nextfloor = nextfloor.nextfloor;
                    }
                    _nextTileIsHoldCachedRef(__instance) = nextfloor.holdLength > -1;
                }

                RunWhileFloorUnchanged(__instance, GetDelegate(ref _checkPostHoldFail, "CheckPostHoldFail"), targetTick);
                RunWhileFloorUnchanged(__instance, GetDelegate(ref _ottoHoldHit, "OttoHoldHit"), targetTick);

                GetDelegate(ref _hitAutoFloors, "HitAutoFloors")(__instance, targetTick);
                GetDelegate(ref _updateHoldBehavior, "UpdateHoldBehavior")(__instance, targetTick);

                RunWhileFloorUnchanged(__instance, GetDelegate(ref _hitHoldFloorsIfStartedAtHold, "HitHoldFloorsIfStartedAtHold"), targetTick);
                RunWhileFloorUnchanged(__instance, GetDelegate(ref _checkPreHoldFail, "CheckPreHoldFail"), targetTick);
                RunWhileFloorUnchanged(__instance, GetDelegate(ref _updateHoldKeys, "UpdateHoldKeys"), targetTick);

                if (RDInput.GetMain(ButtonState.WentUp) > 0)
                {
                    __instance.HitInputEvent(isAuto: false, InputEventState.Up);
                }

                Vector2 topos = ADOBase.controller.camy.topos;
                if (_cachedCamyToPosRef(__instance) != topos)
                {
                    scrPlayer.shouldReplaceCamyToPos = true;
                    scrPlayer.overrideCamyToPos = topos;
                }

                return false;
            }

            /// <summary>Mirror of AsyncInputUtils.WhileFloorNotChange, invoked without lambdas.</summary>
            private static void RunWhileFloorUnchanged(scrPlayer player, Action<scrPlayer, ulong?> action, ulong? tick)
            {
                int num = -1;
                while (num != player.currFloor.seqID)
                {
                    num = player.currFloor.seqID;
                    action(player, tick);
                }
            }
        }
    }
}
