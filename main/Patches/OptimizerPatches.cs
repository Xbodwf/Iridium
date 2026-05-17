using ADOFAI;
using DG.Tweening;
using HarmonyLib;
using Iridium.UI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;
using UnityModManagerNet;

namespace Iridium.Patches
{
    public static class OptimizerPatches
    {
        public static ConcurrentDictionary<string, Vector3> decorRatios = new();
        public static float savedVRAM_MB = 0f;
        private static int processedTextureCount = 0;
        private const int GC_INTERVAL = 50;

        public static void ResetDecorOptimization(bool fullReset)
        {
            decorRatios.Clear();
            savedVRAM_MB = 0f;
            processedTextureCount = 0;
            VRAMNotificationPatch.isFinished = false;
            if (fullReset)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Resources.UnloadUnusedAssets();
            }
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

            private static long EstimateTextureSize(Texture2D tex)
            {
                try
                {
                    var size = Profiler.GetRuntimeMemorySizeLong(tex);
                    if (size > 0) return size;
                }
                catch { }

                var format = tex.format;
                long pixels = (long)tex.width * tex.height;

                if (format == TextureFormat.DXT1 || format == TextureFormat.DXT1Crunched ||
                    format == TextureFormat.ETC_RGB4 || format == TextureFormat.ETC2_RGB)
                    return Math.Max(4L, pixels / 2);

                if (format == TextureFormat.DXT5 || format == TextureFormat.DXT5Crunched ||
                    format == TextureFormat.ETC2_RGBA8)
                    return pixels;

                int fmtVal = (int)format;
                if (fmtVal >= (int)TextureFormat.ASTC_4x4 && fmtVal <= (int)TextureFormat.ASTC_12x12)
                    return Math.Max(16L, pixels / 4);

                if (format == TextureFormat.RGBA32 || format == TextureFormat.ARGB32 ||
                    format == TextureFormat.RGBAFloat || format == TextureFormat.BGRA32)
                    return pixels * 4;

                if (format == TextureFormat.RGB24)
                    return pixels * 3;

                if (format == TextureFormat.Alpha8 || format == TextureFormat.R8)
                    return pixels;

                if (format == TextureFormat.RG16 || format == TextureFormat.R16 ||
                    format == TextureFormat.RGBAHalf || format == TextureFormat.RGHalf || format == TextureFormat.RHalf)
                    return pixels * 2;

                return pixels * 4;
            }

            private static void TryFastCompress(ref Texture2D tex)
            {
                if (Main.Settings.optimizer.dontCompress) return;

                try
                {
                    if (tex.isReadable)
                    {
                        tex.Compress(false);
                        tex.Apply(false, true);
                    }
                }
                catch { }
            }

            private static byte[] ResizeImageBytes(byte[] imageData, double scaleFactor, bool alignTo4, out int originalW, out int originalH, out int newW, out int newH)
            {
                using (var ms = new MemoryStream(imageData))
                using (var bitmap = new System.Drawing.Bitmap(ms))
                {
                    originalW = bitmap.Width;
                    originalH = bitmap.Height;

                    newW = (int)Math.Round(originalW / scaleFactor);
                    newH = (int)Math.Round(originalH / scaleFactor);

                    if (newW < 4) newW = 4;
                    if (newH < 4) newH = 4;

                    const int maxTexSize = 2048;
                    if (newW > maxTexSize || newH > maxTexSize)
                    {
                        double ratio = Math.Min((double)maxTexSize / newW, (double)maxTexSize / newH);
                        newW = (int)Math.Round(newW * ratio);
                        newH = (int)Math.Round(newH * ratio);
                    }

                    if (alignTo4)
                    {
                        newW = ((newW + 3) / 4) * 4;
                        newH = ((newH + 3) / 4) * 4;
                    }

                    bool needsResize = (newW != originalW || newH != originalH);

                    if (!needsResize)
                    {
                        return imageData;
                    }

                    using (var resized = new System.Drawing.Bitmap(newW, newH))
                    {
                        using (var g = System.Drawing.Graphics.FromImage(resized))
                        {
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.DrawImage(bitmap, 0, 0, newW, newH);
                        }

                        using (var outMs = new MemoryStream())
                        {
                            if (imageData.Length > 5 * 1024 * 1024)
                            {
                                var jpegParams = new System.Drawing.Imaging.EncoderParameters(1);
                                jpegParams.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 92L);
                                resized.Save(outMs, GetJpegCodecInfo(), jpegParams);
                            }
                            else
                            {
                                resized.Save(outMs, System.Drawing.Imaging.ImageFormat.Png);
                            }
                            return outMs.ToArray();
                        }
                    }
                }
            }

            private static System.Drawing.Imaging.ImageCodecInfo GetJpegCodecInfo()
            {
                foreach (var codec in System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders())
                {
                    if (codec.MimeType == "image/jpeg")
                        return codec;
                }
                return null;
            }

            [HarmonyPrefix]
            public static bool Prefix(string filePath, ref LoadResult status, ref Texture2D __result)
            {
                byte[] fileData = null;
                byte[] resizedData = null;
                try
                {
                    if (GCS.internalLevelName != null)
                        return true;

                    if (!RDFile.Exists(filePath))
                        return true;

                    if (!Main.Settings.optimizer.enableOptimizer)
                        return true;

                    double scaleFactor = Main.Settings.optimizer.divideBy;
                    bool dontCompress = Main.Settings.optimizer.dontCompress;
                    bool alignTo4 = !Main.Settings.optimizer.dontResizeMultipleOf4;

                    if (scaleFactor <= 1.01 && dontCompress && !alignTo4)
                        return true;

                    fileData = RDFile.ReadAllBytes(filePath, out status);
                    if (fileData == null || fileData.Length == 0)
                        return true;

                    long fileSize = fileData.Length;
                    if (fileSize < 512 * 1024 && scaleFactor <= 1.01 && !alignTo4)
                        return true;

                    long totalAlloc = Profiler.GetTotalAllocatedMemoryLong();
                    int sysMem = SystemInfo.systemMemorySize;
                    long estimatedMem = fileSize * 8L;

                    if (sysMem > 0 && totalAlloc + estimatedMem > sysMem * 1024L * 1024L * 2L / 3L)
                    {
                        Main.Logger?.Log($"[TextureManager] Memory pressure high before {filePath}, forcing GC");
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();
                        Resources.UnloadUnusedAssets();
                        totalAlloc = Profiler.GetTotalAllocatedMemoryLong();
                        if (totalAlloc + estimatedMem > sysMem * 1024L * 1024L * 3L / 4L)
                        {
                            Main.Logger?.Log($"[TextureManager] Memory still critical, creating placeholder for {filePath}");
                            var placeholder = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                            placeholder.name = filePath;
                            placeholder.wrapMode = TextureWrapMode.Repeat;
                            placeholder.Apply(false, true);
                            __result = placeholder;
                            fileData = null;
                            return false;
                        }
                    }

                    int origW = 0, origH = 0, newW = 0, newH = 0;
                    bool taskSuccess = false;

                    var task = Task.Run(() =>
                    {
                        try
                        {
                            resizedData = ResizeImageBytes(fileData, scaleFactor, alignTo4, out origW, out origH, out newW, out newH);
                            taskSuccess = resizedData != null && resizedData.Length > 0;
                        }
                        catch (Exception ex)
                        {
                            Main.Logger?.Log($"[TextureManager] Background resize error for {filePath}: {ex.Message}");
                            taskSuccess = false;
                        }
                    });

                    if (!task.Wait(30000))
                    {
                        Main.Logger?.Log($"[TextureManager] Background resize timed out for {filePath}, creating placeholder");
                        var placeholder = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                        placeholder.name = filePath;
                        placeholder.wrapMode = TextureWrapMode.Repeat;
                        placeholder.Apply(false, true);
                        __result = placeholder;
                        fileData = null;
                        resizedData = null;
                        return false;
                    }

                    fileData = null;

                    if (!taskSuccess || resizedData == null || resizedData.Length == 0)
                    {
                        Main.Logger?.Log($"[TextureManager] Background resize failed for {filePath}, creating placeholder");
                        var placeholder = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                        placeholder.name = filePath;
                        placeholder.wrapMode = TextureWrapMode.Repeat;
                        placeholder.Apply(false, true);
                        __result = placeholder;
                        resizedData = null;
                        return false;
                    }

                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!ImageConversion.LoadImage(tex, resizedData))
                    {
                        UnityEngine.Object.DestroyImmediate(tex);
                        Main.Logger?.Log($"[TextureManager] LoadImage failed for {filePath}, creating placeholder");
                        var placeholder = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                        placeholder.name = filePath;
                        placeholder.wrapMode = TextureWrapMode.Repeat;
                        placeholder.Apply(false, true);
                        __result = placeholder;
                        resizedData = null;
                        return false;
                    }

                    resizedData = null;

                    tex.name = filePath;
                    tex.wrapMode = TextureWrapMode.Repeat;

                    if (!dontCompress && tex.isReadable)
                    {
                        tex.Compress(false);
                        tex.Apply(false, true);
                    }
                    else
                    {
                        tex.Apply(false, false);
                    }

                    if (origW != newW || origH != newH)
                    {
                        decorRatios[filePath] = new Vector3((float)origW / newW, (float)origH / newH, 1f);
                    }

                    if (!Main.Settings.optimizer.dontShowSavedMemory)
                    {
                        long oldSizeEst = (long)origW * origH * 4L;
                        long newSizeEst = Profiler.GetRuntimeMemorySizeLong(tex);
                        if (newSizeEst <= 0) newSizeEst = (long)tex.width * tex.height * 4L;
                        savedVRAM_MB += (oldSizeEst - newSizeEst) / 1048576f;
                    }

                    __result = tex;

                    processedTextureCount++;
                    if (processedTextureCount % GC_INTERVAL == 0)
                    {
                        GC.Collect();
                        Resources.UnloadUnusedAssets();
                    }

                    Main.Logger?.Log($"[TextureManager] Pre-compressed {filePath} from {origW}x{origH} to {newW}x{newH} via background thread");

                    return false;
                }
                catch (Exception ex)
                {
                    Main.Logger?.Log($"[TextureManager] Prefix compression failed for {filePath}: {ex.Message}");
                    fileData = null;
                    resizedData = null;
                    return true;
                }
            }

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
                        Main.Logger?.Log($"[Optimizer] Resizing texture {texName} from {__result.width}x{__result.height} to {newW}x{newH}");

                        long estimatedMem = (long)newW * newH * 4L;
                        long totalAlloc = Profiler.GetTotalAllocatedMemoryLong();
                        int sysMem = SystemInfo.systemMemorySize;
                        if (sysMem > 0 && totalAlloc + estimatedMem * 2 > sysMem * 1024L * 1024L * 3L / 4L)
                        {
                            Main.Logger?.Log($"[Optimizer] Skipping resize for {texName}: memory pressure too high ({totalAlloc / 1048576}MB + {estimatedMem * 2 / 1048576}MB > {sysMem * 3 / 4}MB)");
                            goto skipResize;
                        }

                        if (estimatedMem > 256L * 1024L * 1024L)
                        {
                            Main.Logger?.Log($"[Optimizer] Texture {texName} is very large ({newW}x{newH}, ~{estimatedMem / 1048576}MB), forcing GC before processing");
                            GC.Collect();
                            Resources.UnloadUnusedAssets();
                        }

                        var optimized = CreateProcessedTexture(__result, newW, newH);
                            if (optimized != null)
                            {
                                decorRatios[texName] = new((float)__result.width / newW, (float)__result.height / newH, 1f);
                                if (!Main.Settings.optimizer.dontCompress)
                                {
                                    try
                                    {
                                        optimized.Compress(false);
                                        optimized.Apply(false, true);
                                    }
                                    catch (Exception compressEx)
                                    {
                                        Main.Logger?.Log($"[Optimizer] Compression failed for {texName}: {compressEx.Message}, using uncompressed");
                                        optimized.Apply(false, false);
                                    }
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

                    skipResize:
                    if (!resized)
                    {
                        decorRatios[texName] = Vector3.one;
                        if (!Main.Settings.optimizer.dontCompress)
                        {
                            if (__result.isReadable)
                            {
                                try
                                {
                                    __result.Compress(false);
                                    __result.Apply(false, true);
                                }
                                catch (Exception compressEx)
                                {
                                    Main.Logger?.Log($"[Optimizer] In-place compression failed for {texName}: {compressEx.Message}");
                                }
                            }
                            else if (Main.IsMainThread)
                            {
                                long estimatedMem = (long)__result.width * __result.height * 4L;
                                long totalAlloc = Profiler.GetTotalAllocatedMemoryLong();
                                int sysMem = SystemInfo.systemMemorySize;
                                if (sysMem > 0 && totalAlloc + estimatedMem * 2 > sysMem * 1024L * 1024L * 3L / 4L)
                                {
                                    Main.Logger?.Log($"[Optimizer] Skipping in-place compression for {texName}: memory pressure too high");
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
                                    catch (Exception compressEx)
                                    {
                                        Main.Logger?.Log($"[Optimizer] In-place compression via copy failed for {texName}: {compressEx.Message}");
                                        if (readableCopy != null)
                                            UnityEngine.Object.DestroyImmediate(readableCopy);
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
                    long newSize = 0;
                    try { newSize = Profiler.GetRuntimeMemorySizeLong(__result); } catch { }
                    savedVRAM_MB += (oldSize - newSize) / 1048576f;
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