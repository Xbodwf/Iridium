using ADOFAI;
using DG.Tweening;
using HarmonyLib;
using Iridium.UI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.Profiling;
using UnityModManagerNet;

namespace Iridium.Patches
{
    public static class OptimizerPatches
    {
        public static ConcurrentDictionary<string, Vector3> decorRatios = new();
        public static float savedVRAM_MB = 0f;

        public static void ResetDecorOptimization(bool fullReset)
        {
            if (fullReset) decorRatios.Clear();
            savedVRAM_MB = 0f;
            VRAMNotificationPatch.isFinished = false;
        }

        private static bool TryGetDecorRatio(string id, out Vector3 scale)
        {
            if (!decorRatios.TryGetValue(id, out scale))
            {
                scale = Vector3.one;
                return false;
            }
            return true;
        }

        public static Texture2D? CreateProcessedTexture(Texture2D source, int targetW, int targetH)
        {
            // 检查目标尺寸是否有效
            if (targetW <= 0 || targetH <= 0)
            {
                Main.Logger?.Log($"[Optimizer] Invalid target dimensions: {targetW}x{targetH}");
                return null;
            }

            if (!Main.IsMainThread)
            {
                return ResizeTextureCPU(source, targetW, targetH);
            }

            RenderTexture? rt = null;
            try
            {
                rt = RenderTexture.GetTemporary(targetW, targetH, 0, RenderTextureFormat.ARGB32);
                if (rt == null)
                {
                    Main.Logger?.Log($"[Optimizer] Failed to create RenderTexture for {targetW}x{targetH}");
                    return null;
                }

                rt.filterMode = FilterMode.Bilinear;
                Graphics.Blit(source, rt);

                Texture2D result = new(targetW, targetH, TextureFormat.RGBA32, false);
                RenderTexture.active = rt;
                result.ReadPixels(new Rect(0, 0, targetW, targetH), 0, 0);
                result.Apply(false);
                RenderTexture.active = null;
                result.name = source.name;
                return result;
            }
            catch (Exception e)
            {
                Main.Logger?.Log($"[Optimizer] Error in CreateProcessedTexture: {e.Message}");
                return null;
            }
            finally
            {
                if (rt != null) RenderTexture.ReleaseTemporary(rt);
            }
        }

        private static Texture2D? ResizeTextureCPU(Texture2D source, int targetW, int targetH)
        {
            try
            {
                Color32[] sourcePixels = source.GetPixels32();
                Color32[] targetPixels = new Color32[targetW * targetH];
                int sourceW = source.width;
                int sourceH = source.height;

                float xRatio = (float)(sourceW - 1) / targetW;
                float yRatio = (float)(sourceH - 1) / targetH;

                for (int y = 0; y < targetH; y++)
                {
                    int yFloor = (int)(y * yRatio);
                    float yLerp = (y * yRatio) - yFloor;
                    int y1 = yFloor * sourceW;
                    int y2 = (yFloor + 1) * sourceW;

                    for (int x = 0; x < targetW; x++)
                    {
                        int xFloor = (int)(x * xRatio);
                        float xLerp = (x * xRatio) - xFloor;

                        int index = y * targetW + x;

                        Color32 c1 = sourcePixels[y1 + xFloor];
                        Color32 c2 = sourcePixels[y1 + xFloor + 1];
                        Color32 c3 = sourcePixels[y2 + xFloor];
                        Color32 c4 = sourcePixels[y2 + xFloor + 1];

                        targetPixels[index] = Color32.Lerp(
                            Color32.Lerp(c1, c2, xLerp),
                            Color32.Lerp(c3, c4, xLerp),
                            yLerp
                        );
                    }
                }

                Texture2D result = new(targetW, targetH, source.format, source.mipmapCount > 1);
                result.SetPixels32(targetPixels);
                result.Apply(false, false);
                result.name = source.name;
                return result;
            }
            catch (Exception e)
            {
                Main.Logger?.Log($"[Optimizer] CPU Resize Error: {e.Message}");
                return null;
            }
        }

        [HarmonyPatch(typeof(TextureManager), "LoadTexture")]
        public static class TextureOptimizationPatch
        {
            private static int AlignTo4(int val) => Math.Max(4, (val + 2) & ~3);

            public static void Postfix(ref Texture2D? __result)
            {
                if (__result == null || GCS.internalLevelName != null) return;
                if (__result.width <= 32 || __result.height <= 32) return;
                // if (!Main.IsMainThread) return; // Allow background thread optimization

                long oldSize = 0;
                if (!Main.Settings.optimizer.dontShowSavedMemory)
                {
                    try { oldSize = Profiler.GetRuntimeMemorySizeLong(__result); } catch { }
                }

                string texName = __result.name;
                try
                {
                    double scaleFactor = Main.Settings.optimizer.divideBy;
                    // 只有当缩放比例接近1.0且不压缩纹理时，才跳过优化
                    if (scaleFactor <= 1.01 && Main.Settings.optimizer.dontCompress) return;

                    int newW = (int)Math.Round(__result.width / scaleFactor);
                    int newH = (int)Math.Round(__result.height / scaleFactor);

                    if (newW >= 4 && newH >= 4)
                    {
                        if (!Main.Settings.optimizer.dontResizeMultipleOf4)
                        {
                            newW = AlignTo4(newW);
                            newH = AlignTo4(newH);
                        }
                    }
                    else
                    {
                        newW = __result.width;
                        newH = __result.height;
                    }

                    bool resized = false;
                    if (__result.width != newW || __result.height != newH)
                    {
                        long pixelCount = (long)__result.width * __result.height;
                        if (pixelCount > 16777216)
                        {
                            Main.Logger?.Log($"[Optimizer] Skipping resize for {texName}: too large ({__result.width}x{__result.height})");
                        }
                        else
                        {
                            Main.Logger?.Log($"[Optimizer] Resizing texture {texName} from {__result.width}x{__result.height} to {newW}x{newH}");
                            var optimized = CreateProcessedTexture(__result, newW, newH);
                            if (optimized != null)
                            {
                                decorRatios[texName] = new((float)__result.width / newW, (float)__result.height / newH, 1f);
                                if (!Main.Settings.optimizer.dontCompress)
                                {
                                    optimized.Compress(false);
                                    optimized.Apply(false, true);
                                }
                                else
                                {
                                    optimized.Apply(false, false);
                                }

                                var oldTex = __result;
                                if (Main.IsMainThread)
                                    UnityEngine.Object.DestroyImmediate(oldTex);
                                else
                                    Main.DestroyImmediate(oldTex);

                                __result = optimized;
                                resized = true;
                                Main.Logger?.Log($"[Optimizer] Successfully resized {texName}");
                            }
                            else
                            {
                                Main.Logger?.Log($"[Optimizer] Failed to resize {texName}");
                            }
                        }
                    }

                    if (!resized)
                    {
                        decorRatios[texName] = Vector3.one;
                        if (!Main.Settings.optimizer.dontCompress)
                        {
                            if (__result.isReadable)
                            {
                                __result.Compress(false);
                                __result.Apply(false, true);
                            }
                            else if (Main.IsMainThread)
                            {
                                long pixelCount = (long)__result.width * __result.height;
                                if (pixelCount > 16777216)
                                {
                                    Main.Logger?.Log($"[Optimizer] Skipping compression for {texName}: too large ({__result.width}x{__result.height})");
                                }
                                else
                                {
                                    var readableCopy = new Texture2D(__result.width, __result.height, TextureFormat.RGBA32, __result.mipmapCount > 1);
                                    RenderTexture? rt = null;
                                    try
                                    {
                                        rt = RenderTexture.GetTemporary(__result.width, __result.height, 0, RenderTextureFormat.ARGB32);
                                        Graphics.Blit(__result, rt);
                                        RenderTexture.active = rt;
                                        readableCopy.ReadPixels(new Rect(0, 0, __result.width, __result.height), 0, 0);
                                        RenderTexture.active = null;
                                        readableCopy.Compress(false);
                                        readableCopy.Apply(false, true);
                                        var oldTex = __result;
                                        UnityEngine.Object.DestroyImmediate(oldTex);
                                        __result = readableCopy;
                                    }
                                    finally
                                    {
                                        if (rt != null) RenderTexture.ReleaseTemporary(rt);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Main.Logger?.Log($"[Optimizer] Optimization failed for {texName}: {e.Message}");
                }

                if (!Main.Settings.optimizer.dontShowSavedMemory)
                {
                    try
                    {
                        long newSize = Profiler.GetRuntimeMemorySizeLong(__result);
                        savedVRAM_MB += (oldSize - newSize) / 1048576f;
                    }
                    catch { }
                }
            }
        }

        [HarmonyPatch(typeof(scnGame))]
        public static class OptimizationResetPatches
        {
            [HarmonyPatch("OnDestroy")]
            [HarmonyPatch("LoadAndPlayLevel")]
            [HarmonyPrefix]
            public static void FullReset() => ResetDecorOptimization(true);

            [HarmonyPatch("LoadLevel"), HarmonyPrefix]
            public static void SoftReset() => ResetDecorOptimization(false);

            [HarmonyPatch("Awake"), HarmonyPostfix]
            public static void CleanupTrackCache()
            {
                TrackOptimizationPatches._floorTransformCache = new System.Runtime.CompilerServices.ConditionalWeakTable<scrFloor, Transform>();
            }
        }

        [HarmonyPatch(typeof(scrCustomBackgroundSprite), "SetCustomBG")]
        public static class BackgroundScalingPatch
        {
            public static void Postfix(scrCustomBackgroundSprite __instance)
            {
                if (GCS.internalLevelName != null) return;
                var sprite = __instance.displayedSprite?.sprite;
                if (sprite?.texture == null) return;

                if (TryGetDecorRatio(sprite.texture.name, out Vector3 ratio))
                {
                    __instance.imgSize = Vector2.Scale(__instance.imgSize, ratio);
                }
            }
        }

        [HarmonyPatch(typeof(TextureManager), "ApplyOptionsToTexture")]
        public static class TextureNameCleanupPatch
        {
            public static void Postfix(Texture2D texture)
            {
                if (texture.name.EndsWith("(Clone)"))
                {
                    texture.name = texture.name.Substring(0, texture.name.Length - 7);
                }
            }
        }

        [HarmonyPatch(typeof(scrVisualDecoration), "SetSprite", typeof(TextureManager.CustomSprite), typeof(TextureManager.ImageOptions))]
        public static class DecorationScalingPatch
        {
            private static Func<scrVisualDecoration, Vector2>? _getSpriteUnscaledSize;
            private static Action<scrVisualDecoration, Vector2>? _setSpriteUnscaledSize;
            private static bool _initialized;

            private static void Initialize()
            {
                var prop = AccessTools.Property(typeof(scrVisualDecoration), "spriteUnscaledSize");
                if (prop != null)
                {
                    if (prop.GetMethod != null)
                        _getSpriteUnscaledSize = AccessTools.MethodDelegate<Func<scrVisualDecoration, Vector2>>(prop.GetMethod);
                    if (prop.SetMethod != null)
                        _setSpriteUnscaledSize = AccessTools.MethodDelegate<Action<scrVisualDecoration, Vector2>>(prop.SetMethod);
                }
                _initialized = true;
            }

            public static void Postfix(scrVisualDecoration __instance)
            {
                if (GCS.internalLevelName != null) return;
                var sprite = __instance.spriteRenderer?.sprite;
                if (sprite?.texture == null) return;

                if (TryGetDecorRatio(sprite.texture.name, out Vector3 ratio))
                {
                    if (__instance.spriteRenderer != null) __instance.spriteRenderer.transform.localScale = ratio;

                    if (!Main.Settings.optimizer.dontResizeCollider)
                    {
                        __instance.boxCollider.size = Vector2.Scale(__instance.boxCollider.size, ratio);
                        
                        if (!_initialized) Initialize();
                        if (_getSpriteUnscaledSize != null && _setSpriteUnscaledSize != null)
                        {
                            var oldSize = _getSpriteUnscaledSize(__instance);
                            _setSpriteUnscaledSize(__instance, Vector2.Scale(oldSize, ratio));
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(scrDecorationManager), "UpdateBordersSizes")]
        public static class BorderScalingPatch
        {
            public static void Postfix(scrDecorationManager __instance)
            {
                if (GCS.internalLevelName != null || Main.Settings.optimizer.dontResizeCollider) return;

                var selected = ADOBase.editor?.selectedDecorations;
                if (selected == null || selected.Count == 0) return;

                // 优化：预计算目标集合，避免多次遍历
                var targets = new List<scrVisualDecoration>(selected.Count + 1);
                foreach (var ev in selected)
                {
                    if (ev != null && scrDecorationManager.GetDecoration(ev) is scrVisualDecoration decor)
                        targets.Add(decor);
                }

                // hoveredDecoration 是 LevelEvent 类型，需要转换为 scrDecoration
                var hoveredDecor = __instance.hoveredDecoration != null
                    ? scrDecorationManager.GetDecoration(__instance.hoveredDecoration) as scrVisualDecoration
                    : null;

                if (hoveredDecor != null)
                {
                    if (ADOBase.editor != null && ADOBase.editor.decorations.Contains(__instance.hoveredDecoration))
                    {
                        if (!targets.Contains(hoveredDecor))
                            targets.Add(hoveredDecor);
                    }
                    else
                    {
                        targets.Remove(hoveredDecor);
                    }
                }

                foreach (var decor in targets)
                {
                    if (decor.spriteRenderer?.sprite == null) continue;
                    float ppu = decor.spriteRenderer.sprite.pixelsPerUnit;
                    float offset = 0.5f / ppu;
                    Vector3 baseScale = decor.transform.localScale;
                    Vector2 sign = new(offset * Mathf.Sign(baseScale.x), offset * Mathf.Sign(baseScale.y));
                    Vector3 ratio = decor.spriteRenderer.transform.localScale;
                    decor.bordersRenderer.size = Vector2.Scale(decor.bordersRenderer.size - sign, ratio) + sign;
                    decor.cachedBorderSize = decor.bordersRenderer.size;
                }
            }
        }

        [HarmonyPatch(typeof(scrDecorationManager), "UpdateHitboxSizes")]
        public static class HitboxScalingPatch
        {
            public static void Postfix()
            {
                if (GCS.internalLevelName != null || Main.Settings.optimizer.dontResizeCollider) return;

                var selected = ADOBase.editor?.selectedDecorations;
                if (selected == null || selected.Count == 0) return;

                foreach (var ev in selected)
                {
                    if (ev == null) continue;
                    var decor = scrDecorationManager.GetDecoration(ev) as scrVisualDecoration;
                    if (decor?.spriteRenderer != null)
                    {
                        decor.hitboxRenderer.size = Vector2.Scale(decor.hitboxRenderer.size, decor.spriteRenderer.transform.localScale);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(scrVisualDecoration), "GetDecorationWorldSize")]
        public static class WorldSizeScalingPatch
        {
            public static void Postfix(scrVisualDecoration __instance, ref Vector2 __result)
            {
                if (GCS.internalLevelName != null || Main.Settings.optimizer.dontResizeCollider) return;
                var tex = __instance.spriteRenderer?.sprite?.texture;
                if (tex != null && TryGetDecorRatio(tex.name, out Vector3 ratio))
                {
                    __result = Vector2.Scale(__result, ratio);
                }
            }
        }

        [HarmonyPatch(typeof(scnGame), "ApplyCoreEventsToFloors", typeof(List<scrFloor>), typeof(LevelData), typeof(scrLevelMaker), typeof(List<LevelEvent>), typeof(List<LevelEvent>[]))]
        public static class ShadowOptimizationPatch
        {
            public static void Postfix()
            {
                if (Main.Settings.optimizer.disableShadows)
                {
                    QualitySettings.shadows = ShadowQuality.Disable;
                }
            }
        }

        [HarmonyPatch(typeof(scrVisualDecoration), "Awake")]
        public static class DecorationUpdateOptimizationPatch
        {
            public static void Postfix(scrVisualDecoration __instance)
            {
                if (!Main.Settings.optimizer.optimizeDecorationUpdate) return;
                var sr = __instance.spriteRenderer;
                if (sr != null)
                {
                    sr.receiveShadows = false;
                    sr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                    sr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                }
            }
        }

        [HarmonyPatch(typeof(scrFloor), "Awake")]
        public static class TileUpdateOptimizationPatch
        {
            public static void Postfix(scrFloor __instance)
            {
                if (!Main.Settings.optimizer.optimizeTileUpdate) return;
                var mr = __instance.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.receiveShadows = false;
                    mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                    mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                }
            }
        }

        [HarmonyPatch(typeof(scrVisualDecoration), "UpdateHitbox")]
        public static class VisualDecorationUpdateHitboxPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(scrVisualDecoration __instance)
            {
                if (Main.Settings.optimizer.fastLoading && scnEditor.instance == null)
                {
                    if (__instance.boxCollider != null && __instance.boxCollider.enabled)
                    return false;
                }
                return true;
            }

            [HarmonyPostfix]
            public static void Postfix(scrVisualDecoration __instance)
            {
                if (GCS.internalLevelName != null || !__instance.useHitbox || __instance.spriteRenderer == null) return;
                Vector3 ratio = __instance.spriteRenderer.transform.localScale;
                if (__instance.hitboxType == Hitbox.Box)
                {
                    if (__instance.damageBox != null)
                        __instance.damageBox.size = Vector2.Scale(__instance.damageBox.size, ratio);
                }
                else if (__instance.hitboxType == Hitbox.Capsule)
                {
                    if (__instance.damageCapsule != null)
                        __instance.damageCapsule.size = Vector2.Scale(__instance.damageCapsule.size, ratio);
                }
                else
                {
                    if (__instance.damageCircle != null)
                        __instance.damageCircle.radius *= ratio.x;
                }
            }
        }

        [HarmonyPatch(typeof(scnGame), "ApplyEventsToFloors", typeof(List<scrFloor>), typeof(LevelData), typeof(scrLevelMaker), typeof(List<LevelEvent>))]
        public static class ApplyEventsToFloorsOptimizationPatch
        {
            [HarmonyPrefix]
            public static bool Prefix()
            {
                if (Main.Settings.optimizer.skipEventIfPaused && scrController.instance.paused)
                {
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(scrFloor), "UpdateIconSprite")]
        public static class UpdateIconSpriteOptimizationPatch
        {
            [HarmonyPrefix]
            public static bool Prefix()
            {
                if (Main.Settings.optimizer.optimizeEventIcons && !ADOBase.isLevelEditor && !Main.IsMainThread)
                {
                    return false;
                }
                return true;
            }
        }

        // NOTE: scnGame.Update optimization moved to SceneOptimizationPatches.cs

        [HarmonyPatch(typeof(ffxMoveDecorationsPlus), "StartEffect")]
        public static class MoveDecorationsOptimizationPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(ffxMoveDecorationsPlus __instance)
            {
                if (!Main.Settings.optimizer.optimizeMoveDecorations)
                    return true;

                if (ADOBase.controller.visualQuality == VisualQuality.Low && ADOBase.isOfficialLevel && !ADOBase.levelIsMikoSkip)
                {
                    return false;
                }

                if (!float.IsNaN(__instance.targetScale))
                {
                    __instance.targetScaleV2 = new Vector2(__instance.targetScale, __instance.targetScale);
                }

                __instance.AdjustDurationForHardbake();

                HashSet<scrDecoration> processedDecs = new HashSet<scrDecoration>();
                Vector2 endScale = new Vector2(__instance.targetScaleV2.x, __instance.targetScaleV2.y);
                float duration = __instance.duration;
                bool isZeroDuration = duration <= 0f;

                foreach (string targetTag in __instance.targetTags)
                {
                    if (!__instance.decManager.taggedDecorations.TryGetValue(targetTag, out var decList))
                    {
                        continue;
                    }

                    foreach (scrDecoration dec in decList)
                    {
                        if (!processedDecs.Add(dec))
                        {
                            continue;
                        }

                        Dictionary<TweenType, Tween> tweens = dec.eventTweens;
                        bool isVisual = dec is scrVisualDecoration;
                        bool isParticle = dec is scrParticleDecoration;
                        scrVisualDecoration? visualDec = isVisual ? (scrVisualDecoration)dec : null;

                        if ((bool)ADOBase.customLevel && __instance.movementTypeUsed && __instance.movementType != DecPlacementType.LastPosition)
                        {
                            dec.SetPlacementType(__instance.movementType);
                        }

                        if (!__instance.forceDontTweenMovement)
                        {
                            if (__instance.positionUsed)
                            {
                                Vector2 startPos = (__instance.movementType == DecPlacementType.LastPosition) ? dec.pivotPosVec : dec.startPos;
                                if (!float.IsNaN(__instance.targetPos.x))
                                {
                                    float targetX = startPos.x + __instance.targetPos.x;
                                    if (isZeroDuration)
                                    {
                                        if (tweens.TryGetValue(TweenType.PositionX, out var t)) t.Kill(true);
                                        dec.SetPositionX(targetX, dec.pivotOffsetVec);
                                    }
                                    else
                                    {
                                        if (tweens.TryGetValue(TweenType.PositionX, out var t)) t.Kill(true);
                                        Vector2 newPos = dec.pivotPosVec;
                                        tweens[TweenType.PositionX] = DOTween.To(() => newPos.x, x => newPos.x = x, targetX, duration)
                                            .SetEase(__instance.ease)
                                            .OnUpdate(() => dec.SetPositionX(newPos.x, dec.pivotOffsetVec))
                                            .OnComplete(() => dec.SetPositionX(targetX, dec.pivotOffsetVec))
                                            .Done();
                                    }
                                }

                                if (!float.IsNaN(__instance.targetPos.y))
                                {
                                    float targetY = startPos.y + __instance.targetPos.y;
                                    if (isZeroDuration)
                                    {
                                        if (tweens.TryGetValue(TweenType.PositionY, out var t)) t.Kill(true);
                                        dec.SetPositionY(targetY, dec.pivotOffsetVec);
                                    }
                                    else
                                    {
                                        if (tweens.TryGetValue(TweenType.PositionY, out var t)) t.Kill(true);
                                        Vector2 newPos = dec.pivotPosVec;
                                        tweens[TweenType.PositionY] = DOTween.To(() => newPos.y, y => newPos.y = y, targetY, duration)
                                            .SetEase(__instance.ease)
                                            .OnUpdate(() => dec.SetPositionY(newPos.y, dec.pivotOffsetVec))
                                            .OnComplete(() => dec.SetPositionY(targetY, dec.pivotOffsetVec))
                                            .Done();
                                    }
                                }
                            }

                            if (__instance.parallaxOffsetUsed)
                            {
                                if (!float.IsNaN(__instance.targetParallaxOffset.x))
                                {
                                    if (isZeroDuration)
                                    {
                                        if (tweens.TryGetValue(TweenType.ParallaxOffsetX, out var t)) t.Kill(true);
                                        dec.SetParallaxOffsetX(__instance.targetParallaxOffset.x);
                                    }
                                    else
                                    {
                                        if (tweens.TryGetValue(TweenType.ParallaxOffsetX, out var t)) t.Kill(true);
                                        Vector2 newPos = dec.parallaxOffset;
                                        tweens[TweenType.ParallaxOffsetX] = DOTween.To(() => newPos.x, x => newPos.x = x, __instance.targetParallaxOffset.x, duration)
                                            .SetEase(__instance.ease)
                                            .OnUpdate(() => dec.SetParallaxOffsetX(newPos.x))
                                            .OnComplete(() => dec.SetParallaxOffsetX(__instance.targetParallaxOffset.x))
                                            .Done();
                                    }
                                }

                                if (!float.IsNaN(__instance.targetParallaxOffset.y))
                                {
                                    if (isZeroDuration)
                                    {
                                        if (tweens.TryGetValue(TweenType.ParallaxOffsetY, out var t)) t.Kill(true);
                                        dec.SetParallaxOffsetY(__instance.targetParallaxOffset.y);
                                    }
                                    else
                                    {
                                        if (tweens.TryGetValue(TweenType.ParallaxOffsetY, out var t)) t.Kill(true);
                                        Vector2 newPos = dec.parallaxOffset;
                                        tweens[TweenType.ParallaxOffsetY] = DOTween.To(() => newPos.y, y => newPos.y = y, __instance.targetParallaxOffset.y, duration)
                                            .SetEase(__instance.ease)
                                            .OnUpdate(() => dec.SetParallaxOffsetY(newPos.y))
                                            .OnComplete(() => dec.SetParallaxOffsetY(__instance.targetParallaxOffset.y))
                                            .Done();
                                    }
                                }
                            }

                            if (__instance.pivotUsed)
                            {
                                if (!float.IsNaN(__instance.targetPivot.x))
                                {
                                    if (isZeroDuration)
                                    {
                                        if (tweens.TryGetValue(TweenType.PivotX, out var t)) t.Kill(true);
                                        dec.SetPivotX(__instance.targetPivot.x);
                                    }
                                    else
                                    {
                                        if (tweens.TryGetValue(TweenType.PivotX, out var t)) t.Kill(true);
                                        Vector2 newPivot = dec.pivotOffsetVec;
                                        tweens[TweenType.PivotX] = DOTween.To(() => newPivot.x, x => newPivot.x = x, __instance.targetPivot.x, duration)
                                            .SetEase(__instance.ease)
                                            .OnUpdate(() => dec.SetPivotX(newPivot.x))
                                            .OnComplete(() => dec.SetPivotX(__instance.targetPivot.x))
                                            .Done();
                                    }
                                }

                                if (!float.IsNaN(__instance.targetPivot.y))
                                {
                                    if (isZeroDuration)
                                    {
                                        if (tweens.TryGetValue(TweenType.PivotY, out var t)) t.Kill(true);
                                        dec.SetPivotY(__instance.targetPivot.y);
                                    }
                                    else
                                    {
                                        if (tweens.TryGetValue(TweenType.PivotY, out var t)) t.Kill(true);
                                        Vector2 newPivot = dec.pivotOffsetVec;
                                        tweens[TweenType.PivotY] = DOTween.To(() => newPivot.y, y => newPivot.y = y, __instance.targetPivot.y, duration)
                                            .SetEase(__instance.ease)
                                            .OnUpdate(() => dec.SetPivotY(newPivot.y))
                                            .OnComplete(() => dec.SetPivotY(__instance.targetPivot.y))
                                            .Done();
                                    }
                                }
                            }

                            if (__instance.rotationUsed)
                            {
                                if (isZeroDuration)
                                {
                                    if (tweens.TryGetValue(TweenType.Rotation, out var t)) t.Kill(true);
                                    dec.SetRotation(__instance.targetRot);
                                }
                                else
                                {
                                    if (tweens.TryGetValue(TweenType.Rotation, out var t)) t.Kill(true);
                                    float newRot = dec.rotAngle;
                                    tweens[TweenType.Rotation] = DOTween.To(() => newRot, r => newRot = r, __instance.targetRot, duration)
                                        .SetEase(__instance.ease)
                                        .OnUpdate(() => dec.SetRotation(newRot))
                                        .OnComplete(() => dec.SetRotation(__instance.targetRot))
                                        .Done();
                                }
                            }

                            if (__instance.scaleUsed)
                            {
                                if (!float.IsNaN(endScale.x))
                                {
                                    if (isZeroDuration)
                                    {
                                        if (tweens.TryGetValue(TweenType.ScaleX, out var t)) t.Kill(true);
                                        Vector2 currentScale = dec.scaleVec;
                                        currentScale.x = endScale.x;
                                        dec.SetScale(currentScale);
                                    }
                                    else
                                    {
                                        if (tweens.TryGetValue(TweenType.ScaleX, out var t)) t.Kill(true);
                                        tweens[TweenType.ScaleX] = DOTween.To(() => dec.scaleVec, v => dec.SetScale(v), endScale, duration)
                                            .SetEase(__instance.ease)
                                            .SetOptions(AxisConstraint.X)
                                            .Done();
                                    }
                                }

                                if (!float.IsNaN(endScale.y))
                                {
                                    if (isZeroDuration)
                                    {
                                        if (tweens.TryGetValue(TweenType.ScaleY, out var t)) t.Kill(true);
                                        Vector2 currentScale = dec.scaleVec;
                                        currentScale.y = endScale.y;
                                        dec.SetScale(currentScale);
                                    }
                                    else
                                    {
                                        if (tweens.TryGetValue(TweenType.ScaleY, out var t)) t.Kill(true);
                                        tweens[TweenType.ScaleY] = DOTween.To(() => dec.scaleVec, v => dec.SetScale(v), endScale, duration)
                                            .SetEase(__instance.ease)
                                            .SetOptions(AxisConstraint.Y)
                                            .Done();
                                    }
                                }
                            }
                        }

                        if (__instance.colorUsed)
                        {
                            if (isZeroDuration)
                            {
                                if (tweens.TryGetValue(TweenType.Color, out var t)) t.Kill(true);
                                dec.SetColor(__instance.targetColor);
                            }
                            else
                            {
                                if (tweens.TryGetValue(TweenType.Color, out var t)) t.Kill(true);
                                Color newColor = dec.color;
                                tweens[TweenType.Color] = DOTween.To(() => newColor, c => newColor = c, __instance.targetColor, duration)
                                    .SetEase(__instance.ease)
                                    .OnUpdate(() => dec.SetColor(newColor))
                                    .OnComplete(() => dec.SetColor(__instance.targetColor))
                                    .Done();
                            }
                        }

                        if (__instance.opacityUsed)
                        {
                            if (isZeroDuration)
                            {
                                if (tweens.TryGetValue(TweenType.Opacity, out var t)) t.Kill(true);
                                dec.SetOpacity(__instance.targetOpacity);
                            }
                            else
                            {
                                if (tweens.TryGetValue(TweenType.Opacity, out var t)) t.Kill(true);
                                float newOpacity = dec.opacity;
                                tweens[TweenType.Opacity] = DOTween.To(() => newOpacity, a => newOpacity = a, __instance.targetOpacity, duration)
                                    .SetEase(__instance.ease)
                                    .OnUpdate(() => dec.SetOpacity(newOpacity))
                                    .OnComplete(() => dec.SetOpacity(__instance.targetOpacity))
                                    .Done();
                            }
                        }

                        if (__instance.parallaxUsed)
                        {
                            if (isZeroDuration)
                            {
                                if (tweens.TryGetValue(TweenType.Parallax, out var t)) t.Kill(true);
                                dec.parallax.multiplier = __instance.targetParallax / 100f;
                            }
                            else
                            {
                                if (tweens.TryGetValue(TweenType.Parallax, out var t)) t.Kill(true);
                                Vector2 newParallax = dec.parallax.multiplier;
                                tweens[TweenType.Parallax] = DOTween.To(() => newParallax, p => dec.parallax.multiplier = p, __instance.targetParallax / 100f, duration)
                                    .SetEase(__instance.ease)
                                    .Done();
                            }
                        }

                        if (__instance.visibleUsed)
                        {
                            dec.SetVisible(__instance.visible && !dec.forceHide);
                        }

                        if (__instance.depthUsed)
                        {
                            dec.SetDepth(__instance.targetDepth);
                        }

                        if (isParticle && __instance.imageFilenameUsed)
                        {
                            bool hasImage = !string.IsNullOrEmpty(__instance.targetImageFilename);
                            var customSprites = scrDecorationManager.instance.imageHolder.customSprites;
                            ((scrParticleDecoration)dec).SetSprite(hasImage && customSprites.TryGetValue(__instance.targetImageFilename, out var s) ? s : null);
                        }

                        if (isVisual && visualDec != null)
                        {
                            if (__instance.imageFilenameUsed)
                            {
                                var customSprites = scrDecorationManager.instance.imageHolder.customSprites;
                                customSprites.TryGetValue(__instance.targetImageFilename ?? string.Empty, out var s);
                                visualDec.SetSprite(s, TextureManager.ImageOptions.None);
                            }

                            if (__instance.maskingTypeUsed)
                            {
                                visualDec.SetMaskingType(__instance.targetMaskingType);
                            }

                            if (__instance.maskingTargetUsed)
                            {
                                visualDec.SetMaskingTarget(__instance.targetmaskingTarget);
                            }

                            if (__instance.useMaskingDepthUsed)
                            {
                                visualDec.SetMaskingDepth(__instance.targetUseMaskingDepth);
                            }

                            if (__instance.maskingFrontDepthUsed || __instance.maskingBackDepthUsed)
                            {
                                visualDec.SetMaskingDepth(__instance.maskingFrontDepthUsed ? new int?(__instance.targetMaskingFrontDepth) : null, __instance.maskingBackDepthUsed ? new int?(__instance.targetMaskingBackDepth) : null);
                            }
                        }
                    }
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(scnGame), "UpdateDecorationObjects")]
        public static class VRAMNotificationPatch
        {
            public static bool isFinished = false;
            public static void Postfix(bool reloadDecorations)
            {
                if (GCS.internalLevelName != null || isFinished || !reloadDecorations || Main.Settings.optimizer.dontShowSavedMemory) return;

                if (savedVRAM_MB > 0.1f)
                {
                    UI.VRAMNotificationUI.Show(Localization.Get("SavedMemoryMsg", savedVRAM_MB.ToString("F2")));
                    Main.Logger?.Log(Localization.Get("SavedMemoryLog", savedVRAM_MB.ToString("F2")));
                }
                isFinished = true;
            }
        }

        [HarmonyPatch(typeof(ffxSetFilterPlus), "StartEffect")]
        public static class FilterPlusPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ffxSetFilterPlus __instance)
            {
                if (!Main.Settings.optimizer.optimizeFilters) return;
            }
        }

        [HarmonyPatch(typeof(ffxSetFilterAdvancedPlus), "StartEffect")]
        public static class FilterAdvancedPlusPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ffxSetFilterAdvancedPlus __instance)
            {
                if (!Main.Settings.optimizer.optimizeFilters) return;

                // 优化：直接让原始方法继续执行，但提前做必要检查
                __instance.AdjustDurationForHardbake();
                if (ffxSetFilterAdvancedPlus.blacklistedFilterKeywords.Any(k => __instance.filterName.Contains(k)))
                {
                    // 通过返回 true 让原始方法处理，但已经跳过了黑名单检查
                }
            }
        }
    }
}