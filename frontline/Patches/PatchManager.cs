using HarmonyLib;
using Iridium.Config;
using Iridium.Runtime;
using System;
using System.Collections.Generic;
using System.Reflection;
using static Iridium.Patches.BugfixPatches;

namespace Iridium.Patches
{
	public static class PatchManager
	{
		// Status
		private static readonly Dictionary<Type, bool> _activePatches = new();

		// Patch Declaration
		private class PatchDef
		{
			public Type Type;
			public object Definition;
			public Func<bool> Condition;
			public Type? Parent;
			public string Name;
			public RuntimeKind[] SupportedRuntimes;

			public PatchDef(Type type, Func<bool> condition, Type? parent = null)
			{
				Type = type;
				Definition = type;
				Condition = condition;
				Parent = parent;
				Name = type.Name;
				SupportedRuntimes = new[] { RuntimeKind.Mono };
			}
		}

		private static readonly List<PatchDef> _definitions = new();

		static PatchManager()
		{
			RegisterPatches();
		}

		/// <summary>
		/// Registers a backend-owned patch definition without exposing its concrete
		/// runtime type to Core.
		/// </summary>
		public static void RegisterPatch(string id, object definition, Func<bool> condition)
		{
			if (definition == null) throw new ArgumentNullException(nameof(definition));
			var def = new PatchDef(definition.GetType(), condition)
			{
				Name = id,
				Definition = definition
			};
			if (definition is IPatchDefinition patchDef)
				def.SupportedRuntimes = patchDef.SupportedRuntimes;
			_definitions.Add(def);
		}

		/// <summary>
		/// 注册一个包含 HarmonyPatch 嵌套类型的补丁类中的所有嵌套补丁
		/// </summary>
		private static void RegisterNestedPatches(Type parentType, Func<bool> condition, HashSet<Type>? exclude = null)
		{
			foreach (var type in parentType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
			{
				bool isPatch;
				try
				{
					isPatch = type.GetCustomAttributes(typeof(HarmonyPatch), true).Length > 0;
				}
				catch (Exception error)
				{
					Main.Logger?.Error($"[PatchManager] Skipping {type.FullName}: cannot read Harmony metadata ({error.Message})");
					continue;
				}

				if (isPatch)
				{
					if (exclude != null && exclude.Contains(type))
						continue;
					_definitions.Add(new PatchDef(type, condition));
				}
			}
		}

		private static void RegisterPatches()
		{
			_definitions.Clear();

			// --- Editor Floor Optimization Patches (each with its own sub-condition) ---
			var editorMaster = () => Main.Settings.optimizer.enableEditorFloorOptimization;
			_definitions.Add(new PatchDef(typeof(EditorFloorOptimizationPatches.InsertCharFloorOptimizationPatch),
				() => editorMaster() && Main.Settings.optimizer.incrementalFloorInsert));
			_definitions.Add(new PatchDef(typeof(EditorFloorOptimizationPatches.InsertFloatFloorOptimizationPatch),
				() => editorMaster() && Main.Settings.optimizer.incrementalFloorInsert));
			_definitions.Add(new PatchDef(typeof(EditorFloorOptimizationPatches.InstantiateFloatFloorsOptimizationPatch),
				() => editorMaster() && Main.Settings.optimizer.incrementalFloorInsert));
			_definitions.Add(new PatchDef(typeof(EditorFloorOptimizationPatches.DeleteFloorOptimizationPatch),
				() => editorMaster() && Main.Settings.optimizer.incrementalFloorInsert));
			_definitions.Add(new PatchDef(typeof(EditorFloorOptimizationPatches.RemakePathRedundancyPatch),
				() => editorMaster() && Main.Settings.optimizer.incrementalFloorInsert && Main.Settings.optimizer.skipRedundantRemakePath));
			_definitions.Add(new PatchDef(typeof(EditorFloorOptimizationPatches.GameRemakePathOptimizationPatch),
				() => editorMaster() && Main.Settings.optimizer.incrementalFloorInsert && Main.Settings.optimizer.skipRedundantRemakePath));
			_definitions.Add(new PatchDef(typeof(EditorFloorOptimizationPatches.DrawFloorNumsOptimizationPatch),
				() => editorMaster() && Main.Settings.optimizer.incrementalFloorInsert && Main.Settings.optimizer.rangeBasedRedraw));
			_definitions.Add(new PatchDef(typeof(EditorFloorOptimizationPatches.DrawFloorOffsetLinesOptimizationPatch),
				() => editorMaster() && Main.Settings.optimizer.incrementalFloorInsert && Main.Settings.optimizer.skipRedundantRemakePath));
			_definitions.Add(new PatchDef(typeof(EditorFloorOptimizationPatches.OffsetFloorIDsOptimizationPatch),
				() => editorMaster() && Main.Settings.optimizer.incrementalFloorInsert && Main.Settings.optimizer.optimizeOffsetFloorEvents));
			_definitions.Add(new PatchDef(typeof(EditorFloorOptimizationPatches.SkipApplyEventsOnInsertPatch),
				() => editorMaster() && Main.Settings.optimizer.incrementalFloorInsert && Main.Settings.optimizer.skipApplyEventsOnInsert));

			// --- Optimizer ---
			var optCond = () => Main.Settings.optimizer.enableOptimizer;
			RegisterNestedPatches(typeof(OptimizerPatches), optCond);
			RegisterNestedPatches(typeof(TrackOptimizationPatches), optCond);

			// Adaptive patches — resolve Harmony targets at runtime for cross-version compat
			RegisterPatch("TextureNameCleanup", new AdaptivePatches.TextureNameCleanup(), optCond);
			RegisterPatch("DecorationScalingCustomSprite", new AdaptivePatches.DecorationScalingCustomSprite(), optCond);
			RegisterPatch("DecorationScalingSprite", new AdaptivePatches.DecorationScalingSprite(), optCond);

			// --- Ffx Optimization Patches ---
			RegisterNestedPatches(typeof(FfxOptimizationPatches), optCond);

			// --- Scene Optimization Patches ---
			RegisterNestedPatches(typeof(SceneOptimizationPatches), optCond);

			var eventTweenCond = () => Main.Settings.optimizer.optimizeEventProcessing;
			_definitions.Add(new PatchDef(typeof(EventTweenOptimizationPatches.FfxMoveFloorPlusEventTweensPatch), eventTweenCond));
			_definitions.Add(new PatchDef(typeof(EventTweenOptimizationPatches.FfxMoveDecorationsPlusEventTweensPatch), eventTweenCond));
			_definitions.Add(new PatchDef(typeof(EventTweenOptimizationPatches.FfxRecolorFloorPlusEventTweensPatch), eventTweenCond));
			_definitions.Add(new PatchDef(typeof(EventTweenOptimizationPatches.FfxPlusBaseKillCacheInvalidationPatch), eventTweenCond));

			// --- Loading Optimization Patches ---
			// 排除 FrameSpreadDecorationLoadingPatch，因为它有独立的子开关条件
			RegisterNestedPatches(typeof(LoadingOptimizationPatches), optCond,
				new HashSet<Type> { typeof(LoadingOptimizationPatches.FrameSpreadDecorationLoadingPatch) });
			// 分帧加载需要额外检查 frameSpreadDecorationLoading 子开关
			_definitions.Add(new PatchDef(typeof(LoadingOptimizationPatches.FrameSpreadDecorationLoadingPatch),
				() => Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.frameSpreadDecorationLoading));
			_definitions.Add(new PatchDef(typeof(LoadingOptimizationPatches.FrameSpreadDecorationLoadingPatch.ResetDecorations_Patch),
				() => Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.frameSpreadDecorationLoading));
			_definitions.Add(new PatchDef(typeof(LoadingOptimizationPatches.FrameSpreadDecorationLoadingPatch.Play_Patch),
				() => Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.frameSpreadDecorationLoading));

			// --- DOTween Optimization Patches ---
			// 注意：DOTween优化现在不使用任何HarmonyPatch，只使用运行时配置

			// --- Extreme Optimization Patches ---
			RegisterNestedPatches(typeof(ExtremeOptimizationPatches),
				() => Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.enableExtremeOptimization);

			// --- Tween Safety Patches ---
			var tweenSafetyCond = () => Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.dotweenDefaultRecyclable;
			RegisterNestedPatches(typeof(TweenSafetyPatches), tweenSafetyCond);

			// --- JSON Deserialize Optimization ---
			var jsonOptCond = () => Main.Settings.optimizer.customLevelReadOptimization;
			_definitions.Add(new PatchDef(typeof(JsonPatches.PatchGetCustomLevelName), jsonOptCond));

			// --- Bugfix Patches (2.10.0 only) ---
			_definitions.Add(new PatchDef(typeof(BugfixPatches.PortalTravelFixPatch),
				() => Main.Settings.compatibility.portalTravelFix));
			// v2.10.0+: scrPlayer.marginTracker is now a read-only property that
			// reads directly from scrMistakesManager.marginTrackers[playerID],
			// so SetPlayerCount and Reset sync are handled by the game natively.
			// Always-on: snap offsetTick calibration at start of each level
			_definitions.Add(new PatchDef(typeof(BugfixPatches.AsyncInputPlaySnapPatch), () => true));
			// Fix: ensures hardestDifficulty is reset when playing from editor
			_definitions.Add(new PatchDef(typeof(BugfixPatches.EditorPlayResetMistakesPatch),
				() => Main.Settings.compatibility.fixEditorPlayResetMistakes));
			// Always-on: fixes turnaround detection matching v2.9.8 behavior
			_definitions.Add(new PatchDef(typeof(BugfixPatches.TurnaroundConditionFix),
				() => Main.Settings.compatibility.fixTurnaroundCondition));
			// Always-on: fixes non-coop missAngle not forwarded to Show (non-Perfect rotation missing)
			_definitions.Add(new PatchDef(typeof(JudgeTextPatches.HitTextMeshShowRotationFixPatch), () => Main.Settings.compatibility.fixJudgeRotation));
			// Pause hotkey in editor auto-play (always applied; CheckPauseKey reads the setting at runtime)
			_definitions.Add(new PatchDef(typeof(EditorPausePatches), () => true));
			// Always-on: coop pause beat LockInput shouldn't block other players
			_definitions.Add(new PatchDef(typeof(BugfixPatches.CoopPauseHandleLockFix), () => Main.Settings.compatibility.fixCoopPauseLock));
			_definitions.Add(new PatchDef(typeof(BugfixPatches.CoopPlayerHitFix), () => Main.Settings.compatibility.fixCoopPauseLock));
			_definitions.Add(new PatchDef(typeof(CoopPauseLockFixPlayerPatch),
				() => Main.Settings.compatibility.fixCoopPauseLock && AccessTools.Method(typeof(scrPlayer), "LockInput") != null));
			_definitions.Add(new PatchDef(typeof(CoopPauseLockFixControllerPatch),
				() => Main.Settings.compatibility.fixCoopPauseLock && AccessTools.Method(typeof(scrController), "LockInput") != null));

			// --- UI / Misc ---
			_definitions.Add(new PatchDef(typeof(MiscPatches.RemoveNewsPatch), () => Main.Settings.ui.removeNews));
			_definitions.Add(new PatchDef(typeof(MiscPatches.HideBetaWatermarkPatch), () => Main.Settings.ui.hideBetaWatermark));
			_definitions.Add(new PatchDef(typeof(MiscPatches.ForceDifficultyUIPatch), () => Main.Settings.ui.forceDifficultyUI));
			_definitions.Add(new PatchDef(typeof(MiscPatches.CircleArcPatch), () => Main.Settings.ui.enableCircleArc));
			_definitions.Add(new PatchDef(typeof(MiscPatches.AutoplayTextPositionPatch), () => Main.Settings.ui.moveAutoplayText));
			_definitions.Add(new PatchDef(typeof(MiscPatches.AlwaysCountdownPatch), () => Main.Settings.ui.alwaysCountdown));
			_definitions.Add(new PatchDef(typeof(MiscPatches.AutoplayHintUIPatch), () => true));
			_definitions.Add(new PatchDef(typeof(PausePlanetTrailPatch), () => Main.Settings.ui.enablePausePlanetTrail));

			// Lobby music
			_definitions.Add(new PatchDef(typeof(MiscPatches.LobbyMusicPatch), () => Main.Settings.lobbyMusic.enableLobbyMusicPatch));

            // Compatibility
			var pauseFixCond = () => Main.Settings.compatibility.enableLegacyPauseFix;
			_definitions.Add(new PatchDef(typeof(CompatibilityPatches.LegacyPauseFixPatch_Play), pauseFixCond));
			_definitions.Add(new PatchDef(typeof(CompatibilityPatches.NoFailTooEarlyPatch), () => Main.Settings.compatibility.enableNoFailTooEarly));
			_definitions.Add(new PatchDef(typeof(CompatibilityPatches.ScaleFilterSpeedWithPitchPatch), () => Main.Settings.compatibility.scaleFilterSpeedWithPitch));
			_definitions.Add(new PatchDef(typeof(CameraRelativeDragPatches), () => Main.Settings.compatibility.fixCameraRelativeDrag));
			_definitions.Add(new PatchDef(typeof(JsonPatches.ForceAngleDataPatch), () => Main.Settings.compatibility.forceAngleData));
			_definitions.Add(new PatchDef(typeof(JsonPatches.LegacyBehaviorPatch), () =>
				Main.Settings.compatibility.legacyFlashMode != LegacyBehaviorMode.Default ||
				Main.Settings.compatibility.legacyCamRelativeToMode != LegacyBehaviorMode.Default));

			// Hit Sound
			_definitions.Add(new PatchDef(typeof(HitSoundPatch), () => Main.Settings.hitSound.enableHitSoundPitch));

			// Judge Text
			// InitPatch: Sets template text (may contain {offset} placeholders)
			_definitions.Add(new PatchDef(typeof(JudgeTextPatches.HitTextMeshInitPatch), () => Main.Settings.judgeText.enableJudgeTextCustomization));
			// HitTextManagerShowPatch: Captures missAngle for Show (game doesn't forward it in non-coop)
			_definitions.Add(new PatchDef(typeof(JudgeTextPatches.HitTextManagerShowPatch), () => true));
			// ShowPatch: Replaces {offset} placeholders with calculated timing
			_definitions.Add(new PatchDef(typeof(JudgeTextPatches.HitTextMeshShowPatch), () => Main.Settings.judgeText.enableJudgeTextCustomization));


			// Editor Shortcuts
			_definitions.Add(new PatchDef(typeof(EditorShortcutPatches.EditorShortcutUpdatePatch), () => Main.Settings.editorShortcuts.enableEditorShortcuts));
			_definitions.Add(new PatchDef(typeof(EditorShortcutPatches.FloorSelectCameraJumpPatch), () => Main.Settings.editorShortcuts.enableEditorShortcuts));

			// --- Custom Easing Engine (替代 DOTween) ---
			var easingCond = () => Main.Settings.optimizer.enableCustomEasingEngine;
			RegisterNestedPatches(typeof(CustomEasingPatches), easingCond);

			// ModifyMod
			{
                // Async Input Optimize
                _definitions.Add(new PatchDef(typeof(Iridium.Modules.AsyncInputOptimize.Patch.UnityEngine__SceneManagement__SceneManager), () => Iridium.Main.Settings.asyncInput.enableAIO));
                _definitions.Add(new PatchDef(typeof(Iridium.Modules.AsyncInputOptimize.Patch.__scnGame), () => Iridium.Main.Settings.asyncInput.enableAIO));
                _definitions.Add(new PatchDef(typeof(Iridium.Modules.AsyncInputOptimize.Patch.__scrConductor), () => Iridium.Main.Settings.asyncInput.enableAIO));
                _definitions.Add(new PatchDef(typeof(Iridium.Modules.AsyncInputOptimize.Patch.__scrCountdown), () => Iridium.Main.Settings.asyncInput.enableAIO));
            }
		}

		private sealed class FailureDetail
		{
			public string Name = null!;
			public string State = null!;
			public string Message = null!;
		}

		/// <summary>
		/// 更新所有patch（仅用于初始化或全量更新）
		/// </summary>
		public static void UpdateAllPatches()
		{
			if (Main.RuntimeHost?.PatchBackend == null) return;

			int total = _definitions.Count;
			int succeeded = 0;
			var failures = new List<FailureDetail>();
			var skipNames = new List<string>();

			foreach (var def in _definitions)
			{
				if (!IsRuntimeSupported(def))
				{
					skipNames.Add(def.Name);
					continue;
				}

				var result = UpdateSinglePatch(def);
				if (result == null)
				{
					succeeded++;
				}
				else
				{
					failures.Add(result);
				}
			}

			int applied = succeeded + failures.Count;
			Main.Logger?.Log($"[PatchManager] {succeeded}/{applied} ok, {failures.Count} failed, {skipNames.Count} skipped — open the log file to see detailed errors");

			if (failures.Count > 0)
			{
				Main.Logger?.Log("[PatchManager] --- FAILURES ---");
				foreach (var f in failures)
					Main.Logger?.Log($"[PatchManager]   {f.Name}: {f.State} ({f.Message})");
			}

			if (skipNames.Count > 0)
			{
				Main.Logger?.Log("[PatchManager] --- SKIPPED (unsupported runtime) ---");
				foreach (var name in skipNames)
					Main.Logger?.Log($"[PatchManager]   {name}");
			}
		}

		public static void ReapplyAllPatches()
		{
			if (Main.RuntimeHost?.PatchBackend == null) return;

			Main.RuntimeHost.PatchBackend.SetPerformanceMode(Main.Settings.patchMode.useILPatch);
			Main.RuntimeHost.PatchBackend.RemoveAll();
			_activePatches.Clear();
			UpdateAllPatches();
		}

		/// <summary>
		/// 按类型更新单个patch - 用于增量更新
		/// </summary>
		public static void UpdatePatchByType(Type patchType)
		{
			if (Main.RuntimeHost?.PatchBackend == null) return;

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
			if (Main.RuntimeHost?.PatchBackend == null) return;

			// 优化器相关的 patch 类型
			var optimizerParentTypes = new HashSet<Type>
			{
				typeof(OptimizerPatches),
				typeof(TrackOptimizationPatches),
				typeof(SceneOptimizationPatches),
				typeof(LoadingOptimizationPatches),
				typeof(ExtremeOptimizationPatches),
				typeof(EditorFloorOptimizationPatches)
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
			if (Main.RuntimeHost?.PatchBackend == null) return;

			foreach (var def in _definitions)
			{
				if (predicate(def.Type))
				{
					UpdateSinglePatch(def);
				}
			}
		}

		private static bool IsRuntimeSupported(PatchDef def)
		{
			var runtime = Main.RuntimeHost?.Runtime;
			if (runtime == null || def.SupportedRuntimes == null)
				return true;

			foreach (var kind in def.SupportedRuntimes)
				if (kind == runtime.Value)
					return true;
			return false;
		}

		/// <summary>
		/// 更新单个patch定义。返回 null 表示成功，返回 FailureDetail 表示失败。
		/// </summary>
		private static FailureDetail? UpdateSinglePatch(PatchDef def)
		{
			bool shouldBeActive = CalculateEffectiveStatus(def);
			bool trackedActive = _activePatches.TryGetValue(def.Type, out bool currentActive) && currentActive;

			if (trackedActive != shouldBeActive)
			{
				if (shouldBeActive)
				{
					var result = ApplyPatch(def);
					if (result != null)
					{
						_activePatches[def.Type] = true;
						return null;
					}
					return result;
				}
				else
				{
					var result = RemovePatch(def);
					if (result != null)
					{
						_activePatches.Remove(def.Type);
						return null;
					}
					return result;
				}
			}
			return null; // no-op is success
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

		private static FailureDetail? ApplyPatch(PatchDef def)
		{
			var backend = Main.RuntimeHost?.PatchBackend;
			if (backend == null)
				return new FailureDetail { Name = def.Name, State = "NoBackend", Message = "Patch backend is null" };

			var result = backend.Apply(def.Name, def.Definition);
			if (!result.Succeeded)
				return new FailureDetail { Name = def.Name, State = result.State.ToString(), Message = result.Message };
			return null;
		}

		private static FailureDetail? RemovePatch(PatchDef def)
		{
			var backend = Main.RuntimeHost?.PatchBackend;
			if (backend == null)
				return new FailureDetail { Name = def.Name, State = "NoBackend", Message = "Patch backend is null" };

			var result = backend.Remove(def.Name, def.Definition);
			if (!result.Succeeded)
				return new FailureDetail { Name = def.Name, State = result.State.ToString(), Message = result.Message };
			return null;
		}

		public static void UnpatchAll()
		{
			Main.RuntimeHost?.PatchBackend.RemoveAll();
			_activePatches.Clear();

			Main.Logger?.Log(Localization.Get("PatchManagerUnpatchedAll"));
		}
	}
}
