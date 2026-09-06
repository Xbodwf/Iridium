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
    /// 主纹理赋值。装饰物数量成百上千的谱面上这是一笔稳定的每帧开销。
    ///
    /// 本补丁在装饰物"无滤镜、无遮罩"且（颜色/透明度/平铺/贴图/可见性/启用态）
    /// 全部未变化时跳过整个 UpdateShader —— 帧间静止的装饰物零渲染脚本开销。
    /// 有滤镜的装饰物不过滤（CameraFilterPack 参数可能自行动画，无法廉价判定）。
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
            if (__instance.cfpCache != null && __instance.cfpCache.Length > 0) return true;

            var sprite = __instance.spriteRenderer != null ? __instance.spriteRenderer.sprite : null;
            if (sprite == null) return true;
            if (_meshEnabledRef == null) return true;

            var state = _cache.GetOrCreateValue(__instance);
            bool visible = __instance.GetVisible();
            bool meshEnabled = _meshEnabledRef(__instance);

            bool changed = !state.Valid
                || state.Color != __instance.color
                || state.Opacity != __instance.opacity
                || state.RepeatX != __instance.repeatX
                || state.RepeatY != __instance.repeatY
                || state.Sprite != sprite
                || state.Visible != visible
                || state.MeshEnabled != meshEnabled;

            if (!changed) return false; // 本帧无任何输入变化，跳过 UpdateShader

            state.Valid = true;
            state.Color = __instance.color;
            state.Opacity = __instance.opacity;
            state.RepeatX = __instance.repeatX;
            state.RepeatY = __instance.repeatY;
            state.Sprite = sprite;
            state.Visible = visible;
            state.MeshEnabled = meshEnabled;
            return true;
        }
    }
}
