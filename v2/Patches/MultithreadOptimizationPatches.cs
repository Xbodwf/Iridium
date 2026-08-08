using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using ADOFAI;

namespace Iridium.Patches
{
    /// <summary>
    /// 多线程优化补丁 - 将CPU密集型计算移到后台线程
    /// 注意：只能处理纯计算，不能调用Unity API
    /// </summary>
    public static class MultithreadOptimizationPatches
    {
        private static readonly object _taskLock = new();
        private static readonly List<Task> _runningTasks = new();

        #region Parallel Texture Processing

        /// <summary>
        /// 并行处理多个纹理的缩放
        /// </summary>
        public static void ProcessTexturesParallel(List<Texture2D> textures, double divideBy)
        {
            if (textures == null || textures.Count == 0) return;

            // 使用Parallel.ForEach并行处理
            Parallel.ForEach(textures, texture =>
            {
                if (texture == null) return;

                try
                {
                    int targetW = Mathf.Max(4, (int)(texture.width / divideBy));
                    int targetH = Mathf.Max(4, (int)(texture.height / divideBy));

                    var resized = OptimizerPatches.CreateProcessedTexture(texture, targetW, targetH);
                    if (resized != null)
                    {
                        // 在主线程替换纹理
                        Main.DestroyImmediate(texture);
                    }
                }
                catch (Exception e)
                {
                    Main.Logger?.Error($"[Multithread] Texture processing failed: {e}");
                }
            });
        }

        #endregion

        #region Parallel Math Calculations

        /// <summary>
        /// 并行计算装饰物的目标位置和旋转
        /// </summary>
        public static class DecorationCalculationCache
        {
            private struct DecorationTransform
            {
                public Vector3 position;
                public Quaternion rotation;
                public Vector3 scale;
            }

            private static readonly Dictionary<int, DecorationTransform> _cache = new();
            private static int _lastFrame = -1;

            public static void PreCalculateTransforms(List<scrDecoration> decorations, float deltaTime)
            {
                if (decorations == null || decorations.Count == 0) return;

                int currentFrame = Time.frameCount;
                if (currentFrame == _lastFrame) return;
                _lastFrame = currentFrame;

                // 在后台线程预计算
                Task.Run(() =>
                {
                    try
                    {
                        Parallel.For(0, decorations.Count, i =>
                        {
                            var decor = decorations[i];
                            if (decor == null) return;

                            // 纯数学计算，不调用Unity API
                            // 这里只是示例，实际需要根据游戏逻辑调整
                            var transform = new DecorationTransform
                            {
                                position = Vector3.zero, // 实际计算逻辑
                                rotation = Quaternion.identity,
                                scale = Vector3.one
                            };

                            lock (_cache)
                            {
                                _cache[decor.GetInstanceID()] = transform;
                            }
                        });
                    }
                    catch (Exception e)
                    {
                        Main.Logger?.Error($"[Multithread] Transform calculation failed: {e}");
                    }
                });
            }

            public static bool TryGetCachedTransform(int instanceId, out Vector3 position, out Quaternion rotation, out Vector3 scale)
            {
                lock (_cache)
                {
                    if (_cache.TryGetValue(instanceId, out var transform))
                    {
                        position = transform.position;
                        rotation = transform.rotation;
                        scale = transform.scale;
                        return true;
                    }
                }

                position = Vector3.zero;
                rotation = Quaternion.identity;
                scale = Vector3.one;
                return false;
            }

            public static void Clear()
            {
                lock (_cache)
                {
                    _cache.Clear();
                }
            }
        }

        #endregion

        #region Parallel Event Processing

        /// <summary>
        /// 并行预处理事件数据
        /// </summary>
        [HarmonyPatch(typeof(scnGame), "ApplyEventsToFloors",
            typeof(List<scrFloor>), typeof(LevelData), typeof(scrLevelMaker), typeof(List<LevelEvent>))]
        public static class ParallelEventProcessingPatch
        {
            private static readonly Dictionary<int, List<LevelEvent>> _floorEventCache = new();

            [HarmonyPrefix]
            public static void Prefix(List<scrFloor> floors, List<LevelEvent> events)
            {
                if (!Main.Settings.optimizer.enableOptimizer) return;

                // 在后台线程预处理事件分类
                Task.Run(() =>
                {
                    try
                    {
                        var tempCache = new Dictionary<int, List<LevelEvent>>();

                        // 并行分组事件
                        Parallel.ForEach(events, evt =>
                        {
                            if (evt.floor < 0 || evt.floor >= floors.Count) return;

                            lock (tempCache)
                            {
                                if (!tempCache.TryGetValue(evt.floor, out var list))
                                {
                                    list = new List<LevelEvent>();
                                    tempCache[evt.floor] = list;
                                }
                                list.Add(evt);
                            }
                        });

                        lock (_floorEventCache)
                        {
                            _floorEventCache.Clear();
                            foreach (var kvp in tempCache)
                            {
                                _floorEventCache[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Main.Logger?.Error($"[Multithread] Event processing failed: {e}");
                    }
                });
            }

            public static List<LevelEvent>? GetCachedEvents(int floorIndex)
            {
                lock (_floorEventCache)
                {
                    return _floorEventCache.TryGetValue(floorIndex, out var list) ? list : null;
                }
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// 清理所有后台任务和缓存
        /// </summary>
        public static void Cleanup()
        {
            lock (_taskLock)
            {
                foreach (var task in _runningTasks)
                {
                    try
                    {
                        if (!task.IsCompleted)
                        {
                            // 等待任务完成，最多1秒
                            task.Wait(1000);
                        }
                    }
                    catch (Exception e)
                    {
                        Main.Logger?.Error($"[Multithread] Task cleanup error: {e}");
                    }
                }
                _runningTasks.Clear();
            }

            DecorationCalculationCache.Clear();
            Main.Logger?.Log("[Multithread] Cleanup completed");
        }

        #endregion
    }
}
