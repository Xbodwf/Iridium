using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Iridium
{
    public static class Main
    {
        public static IHandler? Handler { get; private set; }
        public static Harmony? Harmony { get; private set; }
        public static Settings Settings { get; private set; } = null!;
        public static Logger? Logger;
        private static int _mainThreadId;

        public static bool IsMainThread => System.Threading.Thread.CurrentThread.ManagedThreadId == _mainThreadId;

        private static string CurrentVersion => VersionManager.GetFullVersionString();

        public static bool Initialize(IHandler handler)
        {
            _mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            Handler = handler;
            Logger = new Logger();
            Settings = handler.LoadSettings<Settings>();
            Localization.Load();

            handler.OnToggle += OnToggle;
            handler.OnGUI += () => Settings.OnGUI();
            handler.OnSaveGUI += () => handler.SaveSettings(Settings);
            handler.OnUpdate += OnUpdate;

            Harmony = new Harmony(handler.ModId);

            // 预加载 UI 纹理资源，避免首次打开面板时卡顿
            Iridium.UI.IridiumLayout.EnsureTexturesAlive();

            Logger?.Log(Localization.Get("ModLoaded", Settings.language));
            return true;
        }

        private static readonly System.Collections.Concurrent.ConcurrentQueue<System.Action> _actionQueue = new();
        private static System.Collections.Concurrent.ConcurrentQueue<Object> _destroyImmObj = new();

        public static void RunOnMainThread(System.Action action)
        {
            _actionQueue.Enqueue(action);
        }

        public static void DestroyImmediate(Object obj)
        {
            _destroyImmObj.Enqueue(obj);
        }

        private static void OnUpdate(float dt)
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

        private static void OnToggle(bool value)
        {
            if (value)
            {
                Logger?.Log(Localization.Get("ModEnabled"));

                Iridium.Patches.AsyncPatchManager.Start();
                Iridium.Patches.AsyncPatchManager.UpdateAllPatchesAsync();

                if (Main.Settings.optimizer.enableOptimizer)
                {
                    Iridium.Patches.OptimizerPatches.ResetDecorOptimization(true);
                    if (Main.Settings.optimizer.optimizeDOTweenGlobal)
                    {
                        Iridium.Patches.DOTweenOptimizationPatches.ApplyRuntimeSettings();
                    }
                }

                if (Main.Settings.firstRun)
                {
                    UI.MainWindow.ShowFirstRun();
                }
                else if (Main.Settings.lastVersion != CurrentVersion
                    && (string.IsNullOrEmpty(Main.Settings.lastUpgradeMessageSeen_106_beta5)
                        || Main.Settings.lastUpgradeMessageSeen_106_beta5 != "1.0.6_beta5"))
                {
                    UI.MainWindow.ShowUpgrade("UpgradeMessage_1_0_6_beta5");
                }
            }
            else
            {
                Logger?.Log(Localization.Get("ModDisabled"));

                Iridium.Patches.AsyncPatchManager.Stop();
                Iridium.Patches.PatchManager.UnpatchAll();
            }
        }
    }
}
