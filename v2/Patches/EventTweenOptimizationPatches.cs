using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using DG.Tweening;

namespace Iridium.Patches
{
    public static class EventTweenOptimizationPatches
    {
        internal class TweenListHolder
        {
            public List<Tween>? List;
        }

        private static readonly ConditionalWeakTable<ffxPlusBase, TweenListHolder> _cachedTweens = new();

        private static void InvalidateCache(ffxPlusBase instance)
        {
            var holder = _cachedTweens.GetValue(instance, _ => new TweenListHolder());
            holder.List = null;
        }

        private static bool TryGetCached(ffxPlusBase instance, out List<Tween>? list)
        {
            if (_cachedTweens.TryGetValue(instance, out var holder) && holder.List != null)
            {
                list = holder.List;
                return true;
            }
            list = null;
            return false;
        }

        private static void StoreCache(ffxPlusBase instance, IEnumerable<Tween> tweens)
        {
            if (tweens == null) return;
            var holder = _cachedTweens.GetValue(instance, _ => new TweenListHolder());
            holder.List = new List<Tween>(tweens);
        }

        [HarmonyPatch(typeof(ffxMoveFloorPlus), "eventTweens", MethodType.Getter)]
        public static class FfxMoveFloorPlusEventTweensPatch
        {
            [HarmonyPriority(Priority.High)]
            [HarmonyPrefix]
            public static bool Prefix(ffxMoveFloorPlus __instance, ref IEnumerable<Tween> __result)
            {
                if (!Main.Settings.optimizer.optimizeEventProcessing) return true;
                if (TryGetCached(__instance, out var cached))
                {
                    __result = cached!;
                    return false;
                }
                return true;
            }

            [HarmonyPostfix]
            public static void Postfix(ffxMoveFloorPlus __instance, IEnumerable<Tween> __result)
            {
                if (!Main.Settings.optimizer.optimizeEventProcessing) return;
                StoreCache(__instance, __result);
            }
        }

        [HarmonyPatch(typeof(ffxMoveDecorationsPlus), "eventTweens", MethodType.Getter)]
        public static class FfxMoveDecorationsPlusEventTweensPatch
        {
            [HarmonyPriority(Priority.High)]
            [HarmonyPrefix]
            public static bool Prefix(ffxMoveDecorationsPlus __instance, ref IEnumerable<Tween> __result)
            {
                if (!Main.Settings.optimizer.optimizeEventProcessing) return true;
                if (TryGetCached(__instance, out var cached))
                {
                    __result = cached!;
                    return false;
                }
                return true;
            }

            [HarmonyPostfix]
            public static void Postfix(ffxMoveDecorationsPlus __instance, IEnumerable<Tween> __result)
            {
                if (!Main.Settings.optimizer.optimizeEventProcessing) return;
                StoreCache(__instance, __result);
            }
        }

        [HarmonyPatch(typeof(ffxRecolorFloorPlus), "eventTweens", MethodType.Getter)]
        public static class FfxRecolorFloorPlusEventTweensPatch
        {
            [HarmonyPriority(Priority.High)]
            [HarmonyPrefix]
            public static bool Prefix(ffxRecolorFloorPlus __instance, ref IEnumerable<Tween> __result)
            {
                if (!Main.Settings.optimizer.optimizeEventProcessing) return true;
                if (TryGetCached(__instance, out var cached))
                {
                    __result = cached!;
                    return false;
                }
                return true;
            }

            [HarmonyPostfix]
            public static void Postfix(ffxRecolorFloorPlus __instance, IEnumerable<Tween> __result)
            {
                if (!Main.Settings.optimizer.optimizeEventProcessing) return;
                StoreCache(__instance, __result);
            }
        }

        [HarmonyPatch(typeof(ffxPlusBase), "Kill")]
        public static class FfxPlusBaseKillCacheInvalidationPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ffxPlusBase __instance)
            {
                if (!Main.Settings.optimizer.enableOptimizer) return;
                InvalidateCache(__instance);
            }
        }
    }
}
