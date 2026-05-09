using System;
using System.Collections.Generic;
using System.Linq;
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

        // Patch Declaration
        private class PatchDef
        {
            public Type Type;
            public Func<bool> Condition;
            public Type? Parent;
            public string Name;
            public BasePatchMethod? MethodInstance;

            public PatchDef(Type type, Func<bool> condition, Type? parent = null)
            {
                Type = type;
                Condition = condition;
                Parent = parent;
                Name = type.Name;
            }

            /// <summary>为基于 BasePatchMethod 的 patch 创建定义</summary>
            public PatchDef(BasePatchMethod method, Func<bool> condition, Type? parent = null)
            {
                MethodInstance = method;
                Condition = condition;
                Parent = parent;
                Name = method.GetType().Name;
                Type = method.GetType();
            }
        }

        private static readonly List<PatchDef> _definitions = new();

        static PatchManager()
        {
            RegisterPatches();
        }

        private static void RegisterPatches()
        {
            _definitions.Clear();

            // --- Optimizer ---
            var optCond = () => Main.Settings.optimizer.enableOptimizer;
            // Register all nested patch classes in OptimizerPatches
            foreach (var type in typeof(OptimizerPatches).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                // Only register types that have HarmonyPatch attribute
                if (type.GetCustomAttributes(typeof(HarmonyPatch), true).Length > 0)
                {
                    _definitions.Add(new PatchDef(type, optCond));
                }
            }
            _definitions.Add(new PatchDef(typeof(TrackOptimizationPatches), optCond));

            // --- Track optimization StdPatchMethods (replacing attribute-based) ---
            var trackOptCond = () => Main.Settings.optimizer.optimizeMoveTrack;
            _definitions.Add(new PatchDef(new StdMoveFloorPatch(), trackOptCond));
            _definitions.Add(new PatchDef(new StdRecolorFloorPatch(), () => Main.Settings.optimizer.optimizeRecolorTrack));

            // --- Filter optimization StdPatchMethods ---
            var filterOptCond = () => Main.Settings.optimizer.optimizeFilters;
            _definitions.Add(new PatchDef(new StdFilterPlusPatch(), filterOptCond));
            _definitions.Add(new PatchDef(new StdFilterAdvancedPlusPatch(), filterOptCond));

            // --- Ffx Optimization Patches ---
            foreach (var type in typeof(FfxOptimizationPatches).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (type.GetCustomAttributes(typeof(HarmonyPatch), true).Length > 0)
                {
                    _definitions.Add(new PatchDef(type, optCond));
                }
            }
            // DecorationManager LateUpdate optimization (converted to StdPatchMethod)
            _definitions.Add(new PatchDef(new StdDecorationManagerLateUpdatePatch(), optCond));

            // --- Scene Optimization Patches ---
            foreach (var type in typeof(SceneOptimizationPatches).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (type.GetCustomAttributes(typeof(HarmonyPatch), true).Length > 0)
                {
                    _definitions.Add(new PatchDef(type, optCond));
                }
            }

            // --- Loading Optimization Patches ---
            foreach (var type in typeof(LoadingOptimizationPatches).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (type.GetCustomAttributes(typeof(HarmonyPatch), true).Length > 0)
                {
                    _definitions.Add(new PatchDef(type, optCond));
                }
            }

            // --- DOTween Optimization Patches ---
            // 注意：DOTween优化现在不使用任何HarmonyPatch，只使用运行时配置
            // 所以不需要注册任何补丁

            // --- Extreme Optimization Patches ---
            foreach (var type in typeof(ExtremeOptimizationPatches).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (type.GetCustomAttributes(typeof(HarmonyPatch), true).Length > 0)
                {
                    _definitions.Add(new PatchDef(type, () => Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.enableExtremeOptimization));
                }
            }
            // ProcessPendingTweens converted to StdPatchMethod
            _definitions.Add(new PatchDef(new StdProcessPendingTweensPatch(), () => Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.enableExtremeOptimization));

            // --- Tween Safety Patches ---
            // 解决 DOTween.defaultRecyclable=true 时，ADOFAI 中 Tween 引用过期导致的动画异常
            var tweenSafetyCond = () => Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.dotweenDefaultRecyclable;
            foreach (var type in typeof(TweenSafetyPatches).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (type.GetCustomAttributes(typeof(HarmonyPatch), true).Length > 0)
                {
                    _definitions.Add(new PatchDef(type, tweenSafetyCond));
                }
            }

            // --- UI / Misc ---
            _definitions.Add(new PatchDef(typeof(MiscPatches.RemoveNewsPatch), () => Main.Settings.ui.removeNews));
            _definitions.Add(new PatchDef(new StdHideBetaWatermarkPatch(), () => Main.Settings.ui.hideBetaWatermark));
            _definitions.Add(new PatchDef(new StdForceDifficultyUIPatch(), () => Main.Settings.ui.forceDifficultyUI));
            _definitions.Add(new PatchDef(typeof(MiscPatches.CircleArcPatch), () => Main.Settings.ui.enableCircleArc));
            _definitions.Add(new PatchDef(new StdAutoplayTextPositionPatch(), () => Main.Settings.ui.moveAutoplayText));
            _definitions.Add(new PatchDef(typeof(MiscPatches.AlwaysCountdownPatch), () => Main.Settings.ui.alwaysCountdown));

            // Lobby music
            _definitions.Add(new PatchDef(typeof(MiscPatches.LobbyMusicPatch), () => Main.Settings.lobbyMusic.enableLobbyMusicPatch));
            _definitions.Add(new PatchDef(new StdCustomBpmPatch(), () => Main.Settings.lobbyMusic.enableCustomBpm));

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

            // Hit Sound (converted to StdPatchMethod)
            _definitions.Add(new PatchDef(new StdHitSoundPatch(), () => Main.Settings.hitSound.enableHitSoundPitch));

            // Judge Text
            // InitPatch: Handles custom text mode (converted to StdPatchMethod)
            _definitions.Add(new PatchDef(new StdHitTextMeshInitPatch(), () => Main.Settings.judgeText.enableJudgeTextCustomization));
            // ShowPatch: Handles offset mode (converted to StdPatchMethod)
            _definitions.Add(new PatchDef(new StdHitTextMeshShowPatch(), () => Main.Settings.judgeText.enableJudgeTextCustomization && Main.Settings.judgeText.showAsOffset));
            // Rewind: Reset (attribute-based, kept)
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
            // 基于 BasePatchMethod 的 patch（使用 DynamicMethod，不走 Harmony ClassProcessor）
            if (def.MethodInstance != null)
            {
                UpdateSingleMethodPatch(def);
                return;
            }

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

        /// <summary>
        /// 更新基于 BasePatchMethod 的单个 patch
        /// </summary>
        private static void UpdateSingleMethodPatch(PatchDef def)
        {
            bool shouldBeActive = CalculateEffectiveStatus(def);
            bool trackedActive = _activePatches.TryGetValue(def.Type, out bool currentActive) && currentActive;

            if (trackedActive != shouldBeActive)
            {
                Main.Logger?.Log(Localization.Get("PatchManagerStatusChanged", def.Name, trackedActive.ToString(), shouldBeActive.ToString()));
                if (shouldBeActive) def.MethodInstance!.StartPatch();
                else def.MethodInstance!.StopPatch();

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

            // 重置所有 BasePatchMethod 的内部状态（patch 已被 UnpatchAll 移除）
            foreach (var def in _definitions)
            {
                if (def.MethodInstance != null && def.MethodInstance.IsPatched)
                {
                    def.MethodInstance.ForceReset();
                }
            }

            Main.Logger?.Log(Localization.Get("PatchManagerUnpatchedAll"));
        }
    }
}
