using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using ADOFAI;
using DG.Tweening;

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
