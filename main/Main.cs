using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;
using Iridium.UI;
using System.Threading.Tasks;

namespace Iridium
{
    public static class Main
    {
        public static UnityModManager.ModEntry? Mod { get; private set; }
        public static Harmony? Harmony { get; private set; }
        public static Settings Settings { get; private set; } = null!;
        public static Logger? Logger;
        private static int _mainThreadId;

        public static bool IsMainThread => System.Threading.Thread.CurrentThread.ManagedThreadId == _mainThreadId;

        // 当前版本号（用于版本升级检测）
        private static string CurrentVersion => VersionManager.GetFullVersionString();

        private static bool _showFirstRunTips = false;
        private static bool _showUpgradeTips = false;
        private static string _upgradeMessageKey = "";

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            _mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            Mod = modEntry;
            Logger = new Logger(Mod.Logger);
            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            Localization.Load();

            // 检查是否需要显示首次启动提示
            if (Main.Settings.firstRun)
            {
                _showFirstRunTips = true;
            }

            // 检查是否需要显示版本升级提示
            // 只有当从旧版本升级到 beta5 或更高版本时才触发特定提示
            if (!Main.Settings.firstRun && Main.Settings.lastVersion != CurrentVersion)
            {
                // 如果是从 beta5 之前的版本升级到 beta5+
                // 或者未来有新的重大更新提示，可以在这里根据版本号逻辑判断
                // 目前逻辑：只要版本变了且没看过 beta5 的提示，就显示一次
                if (string.IsNullOrEmpty(Main.Settings.lastUpgradeMessageSeen_106_beta5) || Main.Settings.lastUpgradeMessageSeen_106_beta5 != "1.0.6_beta5")
                {
                    _showUpgradeTips = true;
                    _upgradeMessageKey = "UpgradeMessage_1_0_6_beta5";
                }
            }

            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = Main.Settings.OnGUI;
            modEntry.OnSaveGUI = Main.Settings.Save;
            modEntry.OnUpdate = OnUpdate;

            Harmony = new Harmony(modEntry.Info.Id);

            Logger?.Log(Localization.Get("ModLoaded", Main.Settings.language));
            return true;
        }

        private static readonly System.Collections.Concurrent.ConcurrentQueue<System.Action> _actionQueue = new();
        private static volatile System.Collections.Concurrent.ConcurrentQueue<Object> _destroyImmObj = new();

        public static void RunOnMainThread(System.Action action)
        {
            _actionQueue.Enqueue(action);
        }

        public static void DestroyImmediate(Object obj)
        {
            _destroyImmObj.Enqueue(obj);
        }

        private static void OnUpdate(UnityModManager.ModEntry modEntry, float dt)
        {
            while (_destroyImmObj.TryDequeue(out var obj))
            {
                Object.DestroyImmediate(obj);
            }
            while (_actionQueue.TryDequeue(out var action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (System.Exception e)
                {
                    Logger?.Log($"[Main] Error in main thread action: {e}");
                }
            }
            Logger.TaskRun();
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            if (value)
            {
                Logger?.Log(Localization.Get("ModEnabled"));

                // 启动异步 Patch 管理器
                Iridium.Patches.AsyncPatchManager.Start();

                // Strategy: Load only what's needed (lazy loading)
                Iridium.Patches.AsyncPatchManager.UpdateAllPatchesAsync();

                if (Main.Settings.optimizer.enableOptimizer)
                {
                    Iridium.Patches.OptimizerPatches.ResetDecorOptimization(true);
                    // 如果DOTween优化已启用，应用设置
                    if (Main.Settings.optimizer.optimizeDOTweenGlobal)
                    {
                        Iridium.Patches.DOTweenOptimizationPatches.ApplyRuntimeSettings();
                    }
                }

                // 如果需要显示弹窗，使用 MainWindow
                if (_showFirstRunTips)
                {
                    UI.MainWindow.ShowFirstRun();
                }
                else if (_showUpgradeTips)
                {
                    UI.MainWindow.ShowUpgrade(_upgradeMessageKey);
                }
            }
            else
            {
                Logger?.Log(Localization.Get("ModDisabled"));

                // 停止异步 Patch 管理器
                Iridium.Patches.AsyncPatchManager.Stop();

                Iridium.Patches.PatchManager.UnpatchAll();
            }
            return true;
        }
    }
}
