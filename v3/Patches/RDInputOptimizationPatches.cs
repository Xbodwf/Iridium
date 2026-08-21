using Iridium.Config;
using HarmonyLib;
using System;
using System.Collections.Generic;

namespace Iridium.Patches
{
    /// <summary>
    /// Eliminates per-frame List allocations in the RDInput query path.
    ///
    /// Vanilla RDInput.GetStateKeys allocates a fresh List<AnyKeyCode> and copies
    /// the held/pressed key lists into it on every call. scrPlayer.ValidInputWasReleased
    /// calls RDInput.GetMainHeldKeys() every frame per player, and
    /// CountValidKeysPressed / IterateValidKeysHeld call GetMainPressKeys as well.
    ///
    /// This patch replaces GetStateKeys with a pooled implementation: each ButtonState
    /// gets its own reusable [ThreadStatic] List, cleared and refilled on demand. All
    /// call sites consume the returned list immediately within the same method and do
    /// not hold it across frames, so reusing the buffer is safe.
    /// </summary>
    public static class RDInputOptimizationPatches
    {
        [ThreadStatic]
        private static List<AnyKeyCode>? _cachedWentDown;
        [ThreadStatic]
        private static List<AnyKeyCode>? _cachedWentUp;
        [ThreadStatic]
        private static List<AnyKeyCode>? _cachedIsDown;
        [ThreadStatic]
        private static List<AnyKeyCode>? _cachedIsUp;

        private static List<AnyKeyCode> GetPooled(ButtonState state)
        {
            switch (state)
            {
                case ButtonState.WentDown:
                    if (_cachedWentDown == null) _cachedWentDown = new List<AnyKeyCode>();
                    _cachedWentDown.Clear();
                    return _cachedWentDown;
                case ButtonState.WentUp:
                    if (_cachedWentUp == null) _cachedWentUp = new List<AnyKeyCode>();
                    _cachedWentUp.Clear();
                    return _cachedWentUp;
                case ButtonState.IsDown:
                    if (_cachedIsDown == null) _cachedIsDown = new List<AnyKeyCode>();
                    _cachedIsDown.Clear();
                    return _cachedIsDown;
                default:
                    if (_cachedIsUp == null) _cachedIsUp = new List<AnyKeyCode>();
                    _cachedIsUp.Clear();
                    return _cachedIsUp;
            }
        }

        private static List<AnyKeyCode> GetStateKeys(ButtonState state)
        {
            RDInput.GetMain(state);
            var list = GetPooled(state);
            foreach (RDInputType input in RDInput.inputs)
            {
                if (!input.isActive) continue;
                switch (state)
                {
                    case ButtonState.WentDown: list.AddRange(input.pressCount.keys); break;
                    case ButtonState.IsDown: list.AddRange(input.heldCount.keys); break;
                    case ButtonState.WentUp: list.AddRange(input.releaseCount.keys); break;
                    case ButtonState.IsUp: list.AddRange(input.isReleaseCount.keys); break;
                }
            }
            return list;
        }

        [IriPatch(Path = "optimizer/rdInput",
            Pre = typeof(OptimizerSettings),
            Condition = "optimizeRDInputAllocations")]
        [HarmonyPatch(typeof(RDInput), "GetStateKeys")]
        public static class GetStateKeysPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(ButtonState state, ref List<AnyKeyCode> __result)
            {
                // Opt-in optimization — when disabled, fall through to the original.
                if (!Main.Settings.optimizer.optimizeRDInputAllocations)
                {
                    return true;
                }

                __result = GetStateKeys(state);
                return false;
            }
        }
    }
}
