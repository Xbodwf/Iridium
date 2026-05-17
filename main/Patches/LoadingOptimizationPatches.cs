using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using ADOFAI;
using DG.Tweening;
using Iridium.UI;

namespace Iridium.Patches
{
    /// <summary>
    /// 专门优化关卡加载和播放性能的 Patch
    /// 解决：1GB+ 内存占用、加载卡顿、MoveTrack/MoveDecoration 性能问题
    /// </summary>
    public static class LoadingOptimizationPatches
    {
        #region Shared State

        // Tween 对象池
        private static readonly Stack<Tween> _tweenPool = new(100);
        private static readonly object _tweenPoolLock = new();

        // 装饰物批处理状态
        private static bool _isBatchCreating = false;

        // 事件预处理缓存
        private static Dictionary<int, List<LevelEvent>>? _floorEventsCache = null;

        #endregion

        #region Event Processing Optimization

        /// <summary>
        /// 事件预处理 - 缓存每个地板的事件列表
        /// </summary>
        [HarmonyPatch(typeof(scnGame), "ApplyEventsToFloors",
            typeof(List<scrFloor>), typeof(LevelData), typeof(scrLevelMaker), typeof(List<LevelEvent>))]
        public static class EventPreprocessingPatch
        {
            [HarmonyPrefix]
            public static void Prefix(List<scrFloor> floors, List<LevelEvent> events)
            {
                if (!Main.Settings.optimizer.cacheFloorEvents) return;

                try
                {
                    // 预处理：构建地板事件缓存
                    _floorEventsCache = new Dictionary<int, List<LevelEvent>>(floors.Count);

                    foreach (var evt in events)
                    {
                        int floorIndex = evt.floor;
                        if (floorIndex < 0 || floorIndex >= floors.Count) continue;

                        if (!_floorEventsCache.TryGetValue(floorIndex, out var list))
                        {
                            list = new List<LevelEvent>();
                            _floorEventsCache[floorIndex] = list;
                        }

                        list.Add(evt);
                    }

                    Main.Logger?.Log($"[LoadingOptimization] Cached events for {_floorEventsCache.Count} floors");
                }
                catch (Exception e)
                {
                    Main.Logger?.Error($"[LoadingOptimization] Event preprocessing failed: {e}");
                    _floorEventsCache = null;
                }
            }

            [HarmonyPostfix]
            public static void Postfix()
            {
                if (!Main.Settings.optimizer.cacheFloorEvents) return;

                // 清理缓存
                _floorEventsCache = null;
            }

            // 提供静态方法供其他代码使用
            public static List<LevelEvent>? GetCachedEvents(int floorIndex)
            {
                return _floorEventsCache?.TryGetValue(floorIndex, out var list) == true ? list : null;
            }
        }

        #endregion

        #region Tween Optimization

        /// <summary>
        /// Tween 对象池 - 减少 MoveTrack/MoveDecoration 的 GC 压力
        /// </summary>
        public static class TweenPoolManager
        {
            public static Tween? GetTween()
            {
                lock (_tweenPoolLock)
                {
                    if (_tweenPool.Count > 0)
                    {
                        return _tweenPool.Pop();
                    }
                }

                // 池中没有，返回 null，DOTween 会自动创建
                return null;
            }

            public static void ReturnTween(Tween tween)
            {
                if (tween == null) return;

                try
                {
                    tween.Kill(false); // 停止但不完成

                    lock (_tweenPoolLock)
                    {
                        if (_tweenPool.Count < 1000) // 限制池大小
                        {
                            _tweenPool.Push(tween);
                        }
                    }
                }
                catch (Exception e)
                {
                    Main.Logger?.Error($"[LoadingOptimization] Failed to return tween: {e}");
                }
            }

            public static void ClearPool()
            {
                lock (_tweenPoolLock)
                {
                    while (_tweenPool.Count > 0)
                    {
                        var tween = _tweenPool.Pop();
                        tween?.Kill();
                    }
                }
            }
        }

        /// <summary>
        /// 优化 MoveTrack - 减少 Tween 创建
        /// </summary>
        [HarmonyPatch(typeof(ffxMoveFloorPlus), "StartEffect")]
        public static class MoveTrackTweenOptimizationPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ffxMoveFloorPlus __instance)
            {
                if (!Main.Settings.optimizer.optimizeMoveTrackTweens) return;

                // 在 TrackOptimizationPatches 中已有优化
                // 这里可以添加额外的 Tween 池化逻辑
            }
        }

        /// <summary>
        /// 优化 MoveDecoration - 批量处理装饰物动画
        /// </summary>
        [HarmonyPatch(typeof(ffxMoveDecorationsPlus), "StartEffect")]
        public static class MoveDecorationBatchOptimizationPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ffxMoveDecorationsPlus __instance)
            {
                if (!Main.Settings.optimizer.batchMoveDecorations) return;

                // 已在 OptimizerPatches.MoveDecorationsOptimizationPatch 中优化
                // 这里记录性能数据
                Main.Logger?.Log($"[LoadingOptimization] MoveDecoration effect started");
            }
        }

        #endregion

        #region Frame-Spread Decoration Loading

        /// <summary>
        /// 装饰物分帧加载 - 将 UpdateDecorationObjects 中的 CreateDecoration 分散到多帧执行
        /// 防止几千个装饰物在同一帧 Instantiate 导致卡死
        /// </summary>
        [HarmonyPatch(typeof(scnGame), "UpdateDecorationObjects")]
        public static class FrameSpreadDecorationLoadingPatch
        {
            private static readonly Queue<LevelEvent> _pendingDecorations = new();
            private static readonly Queue<LevelEvent> _pendingMoveDecEvents = new();
            private static bool _isLoading = false;
            private static readonly List<GraphicRaycaster> _disabledRaycasters = new();
            public static bool IsLoading => _isLoading;

            [HarmonyPrefix]
            public static bool Prefix(scnGame __instance, bool reloadDecorations)
            {
                if (!Main.Settings.optimizer.enableOptimizer || !Main.Settings.optimizer.frameSpreadDecorationLoading)
                    return true;

                if (_isLoading) return false;

                try
                {
                    var decorations = __instance.decorations;
                    if (decorations == null || decorations.Count == 0)
                        return true;

                    int totalActive = 0;
                    foreach (var dec in decorations)
                    {
                        if (dec.active) totalActive++;
                    }

                    if (totalActive < 100)
                        return true;

                    Main.Logger?.Log($"[LoadingOptimization] Frame-spread loading {totalActive} decorations ({decorations.Count} total)");

                    _isLoading = true;
                    _pendingDecorations.Clear();
                    _pendingMoveDecEvents.Clear();
                    _disabledRaycasters.Clear();

                    foreach (var dec in decorations)
                    {
                        if (dec.active)
                            _pendingDecorations.Enqueue(dec);
                    }

                    foreach (var evt in __instance.events)
                    {
                        if (evt.eventType == LevelEventType.MoveDecorations)
                            _pendingMoveDecEvents.Enqueue(evt);
                    }

                    BlockUIInput();

                    __instance.StartCoroutine(FrameSpreadLoadCoroutine(__instance, reloadDecorations));
                    return false;
                }
                catch (System.Exception ex)
                {
                    Main.Logger?.Error($"[LoadingOptimization] FrameSpreadDecorationLoading failed: {ex}");
                    _isLoading = false;
                    _pendingDecorations.Clear();
                    _pendingMoveDecEvents.Clear();
                    RestoreUIInput();
                    return true;
                }
            }

            private static void BlockUIInput()
            {
                try
                {
                    var canvases = Resources.FindObjectsOfTypeAll<Canvas>();
                    foreach (var canvas in canvases)
                    {
                        var raycaster = canvas.GetComponent<GraphicRaycaster>();
                        if (raycaster != null && raycaster.enabled)
                        {
                            raycaster.enabled = false;
                            _disabledRaycasters.Add(raycaster);
                        }
                    }
                    Main.Logger?.Log($"[LoadingOptimization] Blocked UI input: disabled {_disabledRaycasters.Count} raycaster(s)");
                }
                catch (Exception ex)
                {
                    Main.Logger?.Error($"[LoadingOptimization] Failed to block UI input: {ex}");
                }
            }

            private static void RestoreUIInput()
            {
                try
                {
                    foreach (var raycaster in _disabledRaycasters)
                    {
                        if (raycaster != null)
                            raycaster.enabled = true;
                    }
                    _disabledRaycasters.Clear();
                }
                catch (Exception ex)
                {
                    Main.Logger?.Error($"[LoadingOptimization] Failed to restore UI input: {ex}");
                }
            }

            private static System.Collections.IEnumerator FrameSpreadLoadCoroutine(scnGame instance, bool reloadDecorations)
            {
                int perFrame = Main.Settings.optimizer.decorationsPerFrame;
                if (perFrame < 1) perFrame = 50;

                if (instance == null || instance.decManager == null)
                {
                    _isLoading = false;
                    _pendingDecorations.Clear();
                    _pendingMoveDecEvents.Clear();
                    RestoreUIInput();
                    yield break;
                }

                instance.decManager.ClearDecorations();

                int processed = 0;
                int total = _pendingDecorations.Count;

                UI.VRAMNotificationUI.ShowPersistent(Localization.Get("LoadingDecorationsProgress", 0, total));
                Main.Logger?.Log($"[LoadingOptimization] Starting frame-spread loading: {total} decorations");

                while (_pendingDecorations.Count > 0)
                {
                    if (instance == null || instance.decManager == null)
                    {
                        Main.Logger?.Log($"[LoadingOptimization] scnGame destroyed during loading, aborting");
                        _isLoading = false;
                        _pendingDecorations.Clear();
                        _pendingMoveDecEvents.Clear();
                        RestoreUIInput();
                        yield break;
                    }

                    int batchSize = Mathf.Min(perFrame, _pendingDecorations.Count);
                    for (int i = 0; i < batchSize && _pendingDecorations.Count > 0; i++)
                    {
                        var ev = _pendingDecorations.Dequeue();
                        try
                        {
                            if (reloadDecorations)
                            {
                                bool spritesLoaded = false;
                                instance.decManager.CreateDecoration(ev, out spritesLoaded, -1);
                            }
                            else
                            {
                                object obj;
                                if (ev.data.TryGetValue("decorationImage", out obj))
                                {
                                    string text = (string)obj;
                                    bool isPrefab = text.StartsWith("prefab:", StringComparison.CurrentCultureIgnoreCase);
                                    if (!string.IsNullOrEmpty(text) && !isPrefab && !ADOBase.IsNotAMikoSkipMandatorySprite(text))
                                    {
                                        string filePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(instance.levelPath), text);
                                        LoadResult status;
                                        instance.imgHolder.AddSprite(text, filePath, out status);
                                        if (ADOBase.editor != null)
                                            ADOBase.editor.UpdateImageLoadResult(text, status);
                                    }
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Main.Logger?.Error($"[LoadingOptimization] Failed to create decoration: {ex}");
                        }
                        processed++;
                    }

                    if (_pendingDecorations.Count > 0)
                    {
                        UI.VRAMNotificationUI.UpdateProgress(Localization.Get("LoadingDecorationsProgress", processed, total));
                        yield return null;
                    }
                }

                while (_pendingMoveDecEvents.Count > 0)
                {
                    if (instance == null)
                    {
                        Main.Logger?.Log($"[LoadingOptimization] scnGame destroyed during MoveDecorations loading, aborting");
                        _isLoading = false;
                        _pendingMoveDecEvents.Clear();
                        RestoreUIInput();
                        yield break;
                    }

                    var evt = _pendingMoveDecEvents.Dequeue();
                    try
                    {
                        object obj2;
                        if (evt.data.TryGetValue("decorationImage", out obj2))
                        {
                            string text3 = obj2 as string;
                            if (text3 != null && !string.IsNullOrEmpty(text3))
                            {
                                string filePath2 = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(instance.levelPath), text3);
                                LoadResult status2;
                                instance.imgHolder.AddSprite(text3, filePath2, out status2);
                                if (ADOBase.editor != null)
                                    ADOBase.editor.UpdateImageLoadResult(text3, status2);
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Main.Logger?.Error($"[LoadingOptimization] Failed to load MoveDecorations image: {ex}");
                    }
                }

                _isLoading = false;
                RestoreUIInput();
                Main.Logger?.Log($"[LoadingOptimization] Finished loading {processed} decorations across multiple frames");

                if (reloadDecorations && !Main.Settings.optimizer.dontShowSavedMemory)
                {
                    if (OptimizerPatches.savedVRAM_MB > 0.1f)
                    {
                        UI.VRAMNotificationUI.UpdateProgress(Localization.Get("SavedMemoryMsg", OptimizerPatches.savedVRAM_MB.ToString("F2")));
                        Main.Logger?.Log(Localization.Get("SavedMemoryLog", OptimizerPatches.savedVRAM_MB.ToString("F2")));
                    }
                    else
                    {
                        UI.VRAMNotificationUI.UpdateProgress(Localization.Get("LoadingDecorationsProgress", processed, total));
                    }
                    UI.VRAMNotificationUI.Complete();
                    OptimizerPatches.VRAMNotificationPatch.isFinished = true;
                }
                else
                {
                    UI.VRAMNotificationUI.Complete();
                }
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// 清理缓存和对象池
        /// </summary>
        [HarmonyPatch(typeof(scnGame), "OnDestroy")]
        public static class LoadingOptimizationCleanupPatch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                // 只在启用优化时才清理
                if (!Main.Settings.optimizer.enableOptimizer) return;

                _floorEventsCache?.Clear();
                _floorEventsCache = null;
                _isBatchCreating = false;

                TweenPoolManager.ClearPool();

                Main.Logger?.Log("[LoadingOptimization] Cleaned up caches and pools");
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// 检查是否正在批处理创建装饰物
        /// </summary>
        public static bool IsBatchCreating => _isBatchCreating;

        /// <summary>
        /// 获取缓存的地板事件
        /// </summary>
        public static List<LevelEvent>? GetFloorEvents(int floorIndex)
        {
            return EventPreprocessingPatch.GetCachedEvents(floorIndex);
        }

        #endregion
    }
}
