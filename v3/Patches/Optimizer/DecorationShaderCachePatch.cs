using Iridium.Config;
using HarmonyLib;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Iridium.Patches.Optimizer
{
    /// <summary>
    /// 装饰物渲染脏检查缓存 —— 原版 scrVisualDecoration.UpdateShader 对每个可见
    /// 装饰物每帧无条件执行：材质 SetColor/SetFloat/SetVector、localScale 写入、
    /// 主纹理赋值，以及（带滤镜时）整条滤镜 blit 链。装饰物数量成百上千的谱面
    /// 上这是一笔稳定的每帧开销。
    ///
    /// 无滤镜：颜色/透明度/平铺/贴图/可见性全部未变化时跳过整个 UpdateShader。
    /// 有滤镜（实验性子开关）：滤镜 blit 的输出只取决于（源贴图 × 启用的滤镜集合），
    /// 与装饰物位移和相机无关 —— 以"滤镜签名"参与脏检查，静止时连 blit 链一起跳过，
    /// 直接复用上一帧的 RT 结果。滤镜脚本自行动画参数的画面可能停留在旧状态，
    /// 因此为实验性 opt-in。
    /// </summary>
    [IriPatch(Path = "optimizer/decor", Pre = typeof(OptimizerSettings), Condition = "enableOptimizer,optimizeDecorationShaderCache")]
    [HarmonyPatch(typeof(scrVisualDecoration), "UpdateShader")]
    public static class DecorationShaderCachePatch
    {
        private sealed class CacheState
        {
            public bool Valid;
            public Color Color;
            public float Opacity;
            public float RepeatX, RepeatY;
            public Sprite? Sprite;
            public bool Visible;
            public bool MeshEnabled;
            public int FilterSig;
        }

        private static readonly ConditionalWeakTable<scrVisualDecoration, CacheState> _cache = new();

        private static readonly AccessTools.FieldRef<scrVisualDecoration, bool>? _meshEnabledRef =
            AccessTools.FieldRefAccess<scrVisualDecoration, bool>("meshRendererEnabled");

        private static readonly AccessTools.FieldRef<scrVisualDecoration, MaskingType>? _maskingTypeRef =
            AccessTools.FieldRefAccess<scrVisualDecoration, MaskingType>("maskingType");

        [HarmonyPrefix]
        public static bool Prefix(scrVisualDecoration __instance, bool disable = false)
        {
            if (!Main.Settings.optimizer.optimizeDecorationShaderCache) return true;
            if (disable || __instance.isMask()) return true;
            if (_maskingTypeRef == null || _maskingTypeRef(__instance) != MaskingType.None) return true;

            bool filterCacheOn = Main.Settings.optimizer.optimizeDecorationFilterCache;
            bool hasFilters = __instance.cfpCache != null && __instance.cfpCache.Length > 0;
            if (hasFilters && !filterCacheOn) return true; // 滤镜缓存未开启：带滤镜的照原版每帧执行

            var sprite = __instance.spriteRenderer != null ? __instance.spriteRenderer.sprite : null;
            if (sprite == null) return true;
            if (_meshEnabledRef == null) return true;

            var state = _cache.GetOrCreateValue(__instance);
            bool visible = __instance.GetVisible();
            bool meshEnabled = _meshEnabledRef(__instance);
            int filterSig = hasFilters ? ComputeFilterSignature(__instance.cfpCache!) : 0;

            bool changed = !state.Valid
                || state.Color != __instance.color
                || state.Opacity != __instance.opacity
                || state.RepeatX != __instance.repeatX
                || state.RepeatY != __instance.repeatY
                || state.Sprite != sprite
                || state.Visible != visible
                || state.MeshEnabled != meshEnabled
                || state.FilterSig != filterSig;

            if (!changed) return false; // 本帧无任何输入变化，跳过 UpdateShader（含滤镜 blit 链）

            state.Valid = true;
            state.Color = __instance.color;
            state.Opacity = __instance.opacity;
            state.RepeatX = __instance.repeatX;
            state.RepeatY = __instance.repeatY;
            state.Sprite = sprite;
            state.Visible = visible;
            state.MeshEnabled = meshEnabled;
            state.FilterSig = filterSig;
            return true;
        }

        /// <summary>滤镜签名：类型名 + 启用位。参数自行动画的滤镜无法廉价判定（实验性的原因）。</summary>
        private static int ComputeFilterSignature(MonoBehaviour[] cfpCache)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + cfpCache.Length;
                foreach (var mb in cfpCache)
                {
                    hash = hash * 31 + (mb == null ? 0 : mb.GetType().Name.GetHashCode());
                    hash = hash * 31 + (mb != null && mb.enabled ? 1 : 0);
                }
                return hash;
            }
        }
    }
}
