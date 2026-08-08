using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Iridium.Patches
{
    /// <summary>
    /// 异步 Patch 管理器 - 完全异步执行 Patch 操作，完成后通知 UI
    /// </summary>
    public static class AsyncPatchManager
    {
        private static Thread? _workerThread;
        private static readonly HashSet<Type> _pendingPatchTypes = new();
        private static bool _pendingOptimizerUpdate = false;
        private static bool _pendingAllUpdate = false;
        private static readonly object _queueLock = new();
        private static readonly AutoResetEvent _taskEvent = new(false);
        private static volatile bool _isRunning = false;
        private static volatile bool _isProcessing = false;
        private static readonly Stopwatch _debounceTimer = Stopwatch.StartNew();
        private static long _lastUpdateTimeMs = 0;
        private const int DEBOUNCE_MS = 100; // 防抖延迟100毫秒

        /// <summary>
        /// 是否正在处理 Patch 操作
        /// </summary>
        public static bool IsProcessing => _isProcessing;

        /// <summary>
        /// 启动异步 Patch 处理线程
        /// </summary>
        public static void Start()
        {
            if (_isRunning) return;

            _isRunning = true;
            _isProcessing = false;
            _workerThread = new Thread(WorkerLoop)
            {
                Name = "IridiumPatchWorker",
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };
            _workerThread.Start();

            Main.Logger?.Log(Localization.Get("AsyncPatchWorkerStarted"));
        }

        /// <summary>
        /// 停止异步 Patch 处理线程
        /// </summary>
        public static void Stop()
        {
            if (!_isRunning) return;

            _isRunning = false;
            _taskEvent.Set(); // 唤醒线程以便退出

            if (_workerThread != null && _workerThread.IsAlive)
            {
                _workerThread.Join(1000); // 等待最多1秒
            }

            Main.Logger?.Log(Localization.Get("AsyncPatchWorkerStopped"));
        }

        /// <summary>
        /// 异步更新单个 Patch（带防抖）
        /// </summary>
        public static void UpdatePatchByTypeAsync(Type patchType)
        {
            lock (_queueLock)
            {
                _pendingPatchTypes.Add(patchType);
                _lastUpdateTimeMs = _debounceTimer.ElapsedMilliseconds;
            }
            _taskEvent.Set();
        }

        /// <summary>
        /// 异步更新所有优化器 Patch（带防抖）
        /// </summary>
        public static void UpdateOptimizerPatchesAsync()
        {
            lock (_queueLock)
            {
                _pendingOptimizerUpdate = true;
                _lastUpdateTimeMs = _debounceTimer.ElapsedMilliseconds;
            }
            _taskEvent.Set();
        }

        /// <summary>
        /// 异步更新所有 Patch
        /// </summary>
        public static void UpdateAllPatchesAsync()
        {
            lock (_queueLock)
            {
                _pendingAllUpdate = true;
                _lastUpdateTimeMs = _debounceTimer.ElapsedMilliseconds;
            }
            _taskEvent.Set();
        }

        /// <summary>
        /// 工作线程循环
        /// </summary>
        private static void WorkerLoop()
        {
            while (_isRunning)
            {
                _taskEvent.WaitOne(200); // 最多等待200ms

                if (!_isRunning) break;

                // 检查是否需要执行（防抖）
                bool shouldExecute = false;
                lock (_queueLock)
                {
                    var elapsed = _debounceTimer.ElapsedMilliseconds - _lastUpdateTimeMs;
                    if (elapsed >= DEBOUNCE_MS &&
                        (_pendingAllUpdate || _pendingOptimizerUpdate || _pendingPatchTypes.Count > 0))
                    {
                        shouldExecute = true;
                    }
                }

                if (!shouldExecute) continue;

                // 获取待处理的任务
                bool doAllUpdate = false;
                bool doOptimizerUpdate = false;
                List<Type> patchTypes = new();

                lock (_queueLock)
                {
                    doAllUpdate = _pendingAllUpdate;
                    doOptimizerUpdate = _pendingOptimizerUpdate;
                    patchTypes.AddRange(_pendingPatchTypes);

                    _pendingAllUpdate = false;
                    _pendingOptimizerUpdate = false;
                    _pendingPatchTypes.Clear();
                }

                // 标记为正在处理
                _isProcessing = true;

                // 执行 Patch 操作
                try
                {
                    if (doAllUpdate)
                    {
                        Main.Logger?.Log(Localization.Get("AsyncPatchProcessingAll"));
                        PatchManager.UpdateAllPatches();
                    }
                    else if (doOptimizerUpdate)
                    {
                        Main.Logger?.Log(Localization.Get("AsyncPatchProcessingOptimizer"));
                        PatchManager.UpdateOptimizerPatches();
                    }
                    else if (patchTypes.Count > 0)
                    {
                        Main.Logger?.Log(Localization.Get("AsyncPatchProcessingCount", patchTypes.Count.ToString()));
                        foreach (var type in patchTypes)
                        {
                            PatchManager.UpdatePatchByType(type);
                        }
                    }

                    // 完成后通知主线程刷新 UI
                    // Main.RunOnMainThread(() =>
                    // {
                        // 这里可以触发 UI 刷新，但由于 OnGUI 会自动刷新，所以不需要额外操作
                        Main.Logger?.Log(Localization.Get("AsyncPatchCompleted"));
                    // });
                }
                catch (Exception ex)
                {
                    Main.Logger?.Error(Localization.Get("AsyncPatchError", ex.ToString()));
                }
                finally
                {
                    _isProcessing = false;
                }
            }
        }

        /// <summary>
        /// 获取当前队列中的任务数量
        /// </summary>
        public static int GetPendingTaskCount()
        {
            lock (_queueLock)
            {
                return _pendingPatchTypes.Count +
                       (_pendingOptimizerUpdate ? 1 : 0) +
                       (_pendingAllUpdate ? 1 : 0);
            }
        }
    }
}
