using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Iridium.Config;
using Iridium.Core;

namespace Iridium.Patches
{
    public static class PatchManager
    {
        private static Harmony _harmony => Main.Harmony!;

        // Status
        private static readonly Dictionary<Type, bool> _activePatches = new();
        // Optimization: Cache exact patch bindings for each patch class to speed up and isolate unpatching
        private static readonly Dictionary<Type, List<(MethodBase Original, MethodInfo PatchMethod)>> _patchedBindings = new();

        // Instance-based patches (BasePatchMethod subclasses)
        private static readonly List<BasePatchMethod> _methodPatches = new();

        // Patch Declaration
        private class PatchDef
        {
            public Type Type;
            public Func<bool> Condition;
            public Type? Parent;
            public string Name;

            public PatchDef(Type type, Func<bool> condition, Type? parent = null)
            {
                Type = type;
                Condition = condition;
                Parent = parent;
                Name = type.Name;
            }
        }

        private static readonly List<PatchDef> _definitions = new();

        static PatchManager()
        {
            RegisterPatches();
        }

        /// <summary>
        /// 注册一个实例化补丁（BasePatchMethod 子类）
        /// </summary>
        public static void RegisterMethodPatch(BasePatchMethod patch)
        {
            lock (_methodPatches)
            {
                if (!_methodPatches.Contains(patch))
                    _methodPatches.Add(patch);
            }
        }

        /// <summary>
        /// 取消注册实例化补丁
        /// </summary>
        public static void UnregisterMethodPatch(BasePatchMethod patch)
        {
            lock (_methodPatches)
            {
                _methodPatches.Remove(patch);
            }
        }

        /// <summary>
        /// 注册一个包含 HarmonyPatch 嵌套类型的补丁类中的所有嵌套补丁
        /// </summary>
        private static void RegisterNestedPatches(Type parentType, Func<bool> condition)
        {
            foreach (var type in parentType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (type.GetCustomAttributes(typeof(HarmonyPatch), true).Length > 0)
                    _definitions.Add(new PatchDef(type, condition));
            }
        }

        private static void RegisterPatches()
        {
            _definitions.Clear();

            // --- Optimizer ---
            var optCond = () => Main.Settings.optimizer.enableOptimizer;
            RegisterNestedPatches(typeof(OptimizerPatches), optCond);
            _definitions.Add(new PatchDef(typeof(TrackOptimizationPatches), optCond));

            // --- Ffx Optimization Patches ---
            RegisterNestedPatches(typeof(FfxOptimizationPatches), optCond);

            // --- Scene Optimization Patches ---
            RegisterNestedPatches(typeof(SceneOptimizationPatches), optCond);

            // --- Loading Optimization Patches ---
            RegisterNestedPatches(typeof(LoadingOptimizationPatches), optCond);

            // --- DOTween Optimization Patches ---
            // 注意：DOTween优化现在不使用任何HarmonyPatch，只使用运行时配置

            // --- Extreme Optimization Patches ---
            RegisterNestedPatches(typeof(ExtremeOptimizationPatches),
                () => Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.enableExtremeOptimization);

            // --- Tween Safety Patches ---
            var tweenSafetyCond = () => Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.dotweenDefaultRecyclable;
            RegisterNestedPatches(typeof(TweenSafetyPatches), tweenSafetyCond);

            // --- UI / Misc ---
            _definitions.Add(new PatchDef(typeof(MiscPatches.RemoveNewsPatch), () => Main.Settings.ui.removeNews));
            _definitions.Add(new PatchDef(typeof(MiscPatches.HideBetaWatermarkPatch), () => Main.Settings.ui.hideBetaWatermark));
            _definitions.Add(new PatchDef(typeof(MiscPatches.ForceDifficultyUIPatch), () => Main.Settings.ui.forceDifficultyUI));
            _definitions.Add(new PatchDef(typeof(MiscPatches.CircleArcPatch), () => Main.Settings.ui.enableCircleArc));
            _definitions.Add(new PatchDef(typeof(MiscPatches.AutoplayTextPositionPatch), () => Main.Settings.ui.moveAutoplayText));
            _definitions.Add(new PatchDef(typeof(MiscPatches.AlwaysCountdownPatch), () => Main.Settings.ui.alwaysCountdown));

            // Lobby music
            _definitions.Add(new PatchDef(typeof(MiscPatches.LobbyMusicPatch), () => Main.Settings.lobbyMusic.enableLobbyMusicPatch));

            // Memory
            var memCond = () => Main.Settings.memory.enableMemoryOptimization;
            _definitions.Add(new PatchDef(typeof(MiscPatches.SmartGCPatch), () => memCond() && Main.Settings.memory.enableSmartGC));

            // Compatibility
            var pauseFixCond = () => Main.Settings.compatibility.enableLegacyPauseFix;
            _definitions.Add(new PatchDef(typeof(CompatibilityPatches.LegacyPauseFixPatch_Play), pauseFixCond));
_definitions.Add(new PatchDef(typeof(CompatibilityPatches.LegacyPauseFixPatch_Apply), pauseFixCond));
            _definitions.Add(new PatchDef(typeof(CompatibilityPatches.NoFailTooEarlyPatch), () => Main.Settings.compatibility.enableNoFailTooEarly));
            _definitions.Add(new PatchDef(typeof(JsonPatches.ForceAngleDataPatch), () => Main.Settings.compatibility.forceAngleData));
            _definitions.Add(new PatchDef(typeof(JsonPatches.LegacyBehaviorPatch), () =>
                Main.Settings.compatibility.legacyFlashMode != LegacyBehaviorMode.Default ||
                Main.Settings.compatibility.legacyCamRelativeToMode != LegacyBehaviorMode.Default));

            // Hit Sound
            _definitions.Add(new PatchDef(typeof(HitSoundPatch), () => Main.Settings.hitSound.enableHitSoundPitch));

            // Judge Text
            // InitPatch: Handles custom text mode
            _definitions.Add(new PatchDef(typeof(JudgeTextPatches.HitTextMeshInitPatch), () => Main.Settings.judgeText.enableJudgeTextCustomization));
            // ShowPatch: Handles offset mode
            _definitions.Add(new PatchDef(typeof(JudgeTextPatches.HitTextMeshShowPatch), () => Main.Settings.judgeText.enableJudgeTextCustomization && Main.Settings.judgeText.showAsOffset));
            // Rewind: Reset
            _definitions.Add(new PatchDef(typeof(JudgeTextPatches.ResetTimingOnRewindPatch), () => Main.Settings.judgeText.enableJudgeTextCustomization));
        }

        /// <summary>
        /// 更新所有patch（仅用于初始化或全量更新）
        /// </summary>
        public static void UpdateAllPatches()
        {
            if (_harmony == null) return;

            foreach (var def in _definitions)
            {
                UpdateSinglePatch(def);
            }

            // 同步实例化补丁的 IL 模式
            BasePatchMethod.SyncILModeFromSettings();
        }

        /// <summary>
        /// 按类型更新单个patch - 用于增量更新
        /// </summary>
        public static void UpdatePatchByType(Type patchType)
        {
            if (_harmony == null) return;

            var def = _definitions.Find(d => d.Type == patchType);
            if (def != null)
            {
                UpdateSinglePatch(def);
            }
        }

        /// <summary>
        /// 更新所有优化器相关的patch（当 enableOptimizer 改变时调用）
        /// </summary>
        public static void UpdateOptimizerPatches()
        {
            if (_harmony == null) return;

            // 优化器相关的 patch 类型
            var optimizerParentTypes = new HashSet<Type>
            {
                typeof(OptimizerPatches),
                typeof(TrackOptimizationPatches),
                typeof(SceneOptimizationPatches),
                typeof(LoadingOptimizationPatches),
                typeof(ExtremeOptimizationPatches)
            };

            foreach (var def in _definitions)
            {
                // 检查是否是优化器相关的 patch
                bool isOptimizerPatch = optimizerParentTypes.Contains(def.Type) ||
                    (def.Type.DeclaringType != null && optimizerParentTypes.Contains(def.Type.DeclaringType));

                if (isOptimizerPatch)
                {
                    UpdateSinglePatch(def);
                }
            }
        }

        /// <summary>
        /// 更新满足条件的patch - 用于批量增量更新
        /// </summary>
        public static void UpdatePatchesByCondition(Func<Type, bool> predicate)
        {
            if (_harmony == null) return;

            foreach (var def in _definitions)
            {
                if (predicate(def.Type))
                {
                    UpdateSinglePatch(def);
                }
            }
        }

        /// <summary>
        /// 更新单个patch定义
        /// </summary>
        private static void UpdateSinglePatch(PatchDef def)
        {
            bool shouldBeActive = CalculateEffectiveStatus(def);
            bool trackedActive = _activePatches.TryGetValue(def.Type, out bool currentActive) && currentActive;

            if (trackedActive != shouldBeActive)
            {
                Main.Logger?.Log(Localization.Get("PatchManagerStatusChanged", def.Name, trackedActive.ToString(), shouldBeActive.ToString()));
                if (shouldBeActive) ApplyPatch(def.Type);
                else RemovePatch(def.Type);

                _activePatches[def.Type] = shouldBeActive;
            }
        }

        private static bool CalculateEffectiveStatus(PatchDef def)
        {
            // Condition
            if (!def.Condition()) return false;

            // Check Parent
            if (def.Parent != null)
            {
                _activePatches.TryGetValue(def.Parent, out bool parentActive);
                if (!parentActive) return false;
            }

            return true;
        }

        // 移除不再使用的 IsActuallyPatched 辅助方法

        private static void ApplyPatch(Type type)
        {
            try
            {
                Main.Logger?.Log(Localization.Get("PatchManagerAttemptApply", type.Name));
                var processor = _harmony.CreateClassProcessor(type);
                var originals = processor.Patch();

                if (originals != null && originals.Count > 0)
                {
                    var bindings = new List<(MethodBase Original, MethodInfo PatchMethod)>();
                    foreach (var original in originals)
                    {
                        var info = Harmony.GetPatchInfo(original);
                        if (info == null) continue;

                        foreach (var p in info.Prefixes)
                        {
                            if (p.owner == _harmony.Id && p.PatchMethod.DeclaringType == type)
                                bindings.Add((original, p.PatchMethod));
                        }
                        foreach (var p in info.Postfixes)
                        {
                            if (p.owner == _harmony.Id && p.PatchMethod.DeclaringType == type)
                                bindings.Add((original, p.PatchMethod));
                        }
                        foreach (var p in info.Transpilers)
                        {
                            if (p.owner == _harmony.Id && p.PatchMethod.DeclaringType == type)
                                bindings.Add((original, p.PatchMethod));
                        }
                        foreach (var p in info.Finalizers)
                        {
                            if (p.owner == _harmony.Id && p.PatchMethod.DeclaringType == type)
                                bindings.Add((original, p.PatchMethod));
                        }
                    }

                    _patchedBindings[type] = bindings;
                    _activePatches[type] = true;
                    Main.Logger?.Log(Localization.Get("PatchManagerSuccessApply", type.Name, bindings.Count.ToString()));
                }
                else
                {
                    Main.Logger?.Log(Localization.Get("PatchManagerNoMethods", type.Name));
                }
            }
            catch (Exception e)
            {
                Main.Logger?.Error(Localization.Get("PatchManagerFailedApply", type.Name, e.ToString()));
            }
        }

        private static void RemovePatch(Type type)
        {
            try
            {
                Main.Logger?.Log(Localization.Get("PatchManagerAttemptRemove", type.Name));
                if (_patchedBindings.TryGetValue(type, out var bindings) && bindings.Count > 0)
                {
                    Main.Logger?.Log(Localization.Get("PatchManagerUsingCache", type.Name, bindings.Count.ToString()));
                    foreach (var (original, patchMethod) in bindings)
                    {
                        _harmony.Unpatch(original, patchMethod);
                    }
                    _patchedBindings.Remove(type);
                }
                else
                {
                    // Fallback to slow method if cache is missing or empty
                    Main.Logger?.Log(Localization.Get("PatchManagerUsingFallback", type.Name));
                    UnpatchMethod(type);
                    _patchedBindings.Remove(type);
                }

                _activePatches[type] = false;
                Main.Logger?.Log(Localization.Get("PatchManagerSuccessRemove", type.Name));
            }
            catch (Exception e)
            {
                Main.Logger?.Error(Localization.Get("PatchManagerFailedRemove", type.Name, e.ToString()));
            }
        }

        private static void UnpatchMethod(Type patchClass)
        {
            // Slow fallback: search all patched methods in the game
            var allPatchedMethods = _harmony.GetPatchedMethods();
            foreach (var original in allPatchedMethods)
            {
                var info = Harmony.GetPatchInfo(original);
                if (info == null) continue;

                foreach (var p in info.Prefixes)
                {
                    if (p.owner == _harmony.Id && p.PatchMethod.DeclaringType == patchClass)
                        _harmony.Unpatch(original, p.PatchMethod);
                }
                foreach (var p in info.Postfixes)
                {
                    if (p.owner == _harmony.Id && p.PatchMethod.DeclaringType == patchClass)
                        _harmony.Unpatch(original, p.PatchMethod);
                }
                foreach (var p in info.Transpilers)
                {
                    if (p.owner == _harmony.Id && p.PatchMethod.DeclaringType == patchClass)
                        _harmony.Unpatch(original, p.PatchMethod);
                }
                foreach (var p in info.Finalizers)
                {
                    if (p.owner == _harmony.Id && p.PatchMethod.DeclaringType == patchClass)
                        _harmony.Unpatch(original, p.PatchMethod);
                }
            }
        }

        public static void UnpatchAll()
        {
            _harmony?.UnpatchAll(_harmony.Id);
            _activePatches.Clear();
            _patchedBindings.Clear();

            // 停止所有实例化补丁
            lock (_methodPatches)
            {
                foreach (var mp in _methodPatches)
                {
                    if (mp.IsPatched)
                        mp.StopPatch();
                }
            }

            Main.Logger?.Log(Localization.Get("PatchManagerUnpatchedAll"));
        }
    }
}
