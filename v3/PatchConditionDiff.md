# Patch Condition Diff: Current vs Old

## Format
| # | Patch Type | Current Path | Current Condition | Old Condition | Status | Action Needed |

---

## Mismatches Found

### Category 1: Wrong condition (critical) — Missing parent/base conditions

These patches are missing the parent `enableOptimizer` condition that was part of their old compound condition.

| # | Patch Type | Current Path | Current Condition | Old Condition | Status | Action Needed |
|---|---|---|---|---|---|---|
| 1 | `ExtremeOptimizationPatches.ExtremeMoveFloorPatch` | `optimizer/extreme` | `enableExtremeOptimization` | `enableOptimizer && enableExtremeOptimization` | ❌ WRONG | Fix: `enableOptimizer && enableExtremeOptimization` |
| 2 | `ExtremeOptimizationPatches.ExtremeMoveDecorPatch` | `optimizer/extreme` | `enableExtremeOptimization` | `enableOptimizer && enableExtremeOptimization` | ❌ WRONG | Fix: `enableOptimizer && enableExtremeOptimization` |
| 3 | `TweenSafetyPatches.ClearPausedTweensOnReset` | `optimizer/tweenSafety` | `dotweenDefaultRecyclable` | `enableOptimizer && dotweenDefaultRecyclable` | ❌ WRONG | Fix: `enableOptimizer && dotweenDefaultRecyclable` |
| 4 | `TweenSafetyPatches.SafeDecorationOnDestroy` | `optimizer/tweenSafety` | `dotweenDefaultRecyclable` | `enableOptimizer && dotweenDefaultRecyclable` | ❌ WRONG | Fix: `enableOptimizer && dotweenDefaultRecyclable` |
| 5 | `TweenSafetyPatches.SafeFfxPlusBaseKill` | `optimizer/tweenSafety` | `dotweenDefaultRecyclable` | `enableOptimizer && dotweenDefaultRecyclable` | ❌ WRONG | Fix: `enableOptimizer && dotweenDefaultRecyclable` |
| 6 | `TweenSafetyPatches.SafeFfxPlusBaseScrubToTime` | `optimizer/tweenSafety` | `dotweenDefaultRecyclable` | `enableOptimizer && dotweenDefaultRecyclable` | ❌ WRONG | Fix: `enableOptimizer && dotweenDefaultRecyclable` |
| 7 | `LoadingOptimizationPatches.FrameSpreadDecorationLoadingPatch` | `optimizer/loading` | `frameSpreadDecorationLoading` | `enableOptimizer && frameSpreadDecorationLoading` | ❌ WRONG | Fix: `enableOptimizer && frameSpreadDecorationLoading` |
| 8 | `LoadingOptimizationPatches.FrameSpreadDecorationLoadingPatch.ReloadAssets_Patch` | `optimizer/loading` | `frameSpreadDecorationLoading` | `enableOptimizer && frameSpreadDecorationLoading` | ❌ WRONG | Fix: `enableOptimizer && frameSpreadDecorationLoading` |
| 9 | `LoadingOptimizationPatches.FrameSpreadDecorationLoadingPatch.ResetDecorations_Patch` | `optimizer/loading` | `frameSpreadDecorationLoading` | `enableOptimizer && frameSpreadDecorationLoading` | ❌ WRONG | Fix: `enableOptimizer && frameSpreadDecorationLoading` |
| 10 | `LoadingOptimizationPatches.FrameSpreadDecorationLoadingPatch.Play_Patch` | `optimizer/loading` | `frameSpreadDecorationLoading` | `enableOptimizer && frameSpreadDecorationLoading` | ❌ WRONG | Fix: `enableOptimizer && frameSpreadDecorationLoading` |

### Category 2: Wrong condition — EditorFloorOptimization patches (entirely wrong base)

These patches used `enableOptimizer` or just the sub-condition, but the old system used `enableEditorFloorOptimization` as the master condition with various sub-conditions.

| # | Patch Type | Current Path | Current Condition | Old Condition | Status | Action Needed |
|---|---|---|---|---|---|---|
| 11 | `EditorFloorOptimizationPatches.InsertCharFloorOptimizationPatch` | `optimizer/editorFloor/insert` | `enableOptimizer` | `enableEditorFloorOptimization && incrementalFloorInsert` | ❌ WRONG | Fix: `enableEditorFloorOptimization && incrementalFloorInsert` |
| 12 | `EditorFloorOptimizationPatches.InsertFloatFloorOptimizationPatch` | `optimizer/editorFloor/insert` | `enableOptimizer` | `enableEditorFloorOptimization && incrementalFloorInsert` | ❌ WRONG | Fix: `enableEditorFloorOptimization && incrementalFloorInsert` |
| 13 | `EditorFloorOptimizationPatches.InstantiateFloatFloorsOptimizationPatch` | `optimizer/editorFloor/insert` | `enableOptimizer` | `enableEditorFloorOptimization && incrementalFloorInsert` | ❌ WRONG | Fix: `enableEditorFloorOptimization && incrementalFloorInsert` |
| 14 | `EditorFloorOptimizationPatches.DeleteFloorOptimizationPatch` | `optimizer/editorFloor/insert` | `enableOptimizer` | `enableEditorFloorOptimization && incrementalFloorInsert` | ❌ WRONG | Fix: `enableEditorFloorOptimization && incrementalFloorInsert` |
| 15 | `EditorFloorOptimizationPatches.RemakePathRedundancyPatch` | `optimizer/editorFloor/insert` | `skipRedundantRemakePath` | `enableEditorFloorOptimization && incrementalFloorInsert && skipRedundantRemakePath` | ❌ WRONG | Fix: `enableEditorFloorOptimization && incrementalFloorInsert && skipRedundantRemakePath` |
| 16 | `EditorFloorOptimizationPatches.GameRemakePathOptimizationPatch` | `optimizer/editorFloor/insert` | `skipRedundantRemakePath` | `enableEditorFloorOptimization && incrementalFloorInsert && skipRedundantRemakePath` | ❌ WRONG | Fix: `enableEditorFloorOptimization && incrementalFloorInsert && skipRedundantRemakePath` |
| 17 | `EditorFloorOptimizationPatches.DrawFloorNumsOptimizationPatch` | `optimizer/editorFloor/insert` | `rangeBasedRedraw` | `enableEditorFloorOptimization && incrementalFloorInsert && rangeBasedRedraw` | ❌ WRONG | Fix: `enableEditorFloorOptimization && incrementalFloorInsert && rangeBasedRedraw` |
| 18 | `EditorFloorOptimizationPatches.DrawFloorOffsetLinesOptimizationPatch` | `optimizer/editorFloor/insert` | `skipRedundantRemakePath` | `enableEditorFloorOptimization && incrementalFloorInsert && skipRedundantRemakePath` | ❌ WRONG | Fix: `enableEditorFloorOptimization && incrementalFloorInsert && skipRedundantRemakePath` |
| 19 | `EditorFloorOptimizationPatches.OffsetFloorIDsOptimizationPatch` | `optimizer/editorFloor/insert` | `optimizeOffsetFloorEvents` | `enableEditorFloorOptimization && incrementalFloorInsert && optimizeOffsetFloorEvents` | ❌ WRONG | Fix: `enableEditorFloorOptimization && incrementalFloorInsert && optimizeOffsetFloorEvents` |
| 20 | `EditorFloorOptimizationPatches.SkipApplyEventsOnInsertPatch` | `optimizer/editorFloor/insert` | `skipApplyEventsOnInsert` | `enableEditorFloorOptimization && incrementalFloorInsert && skipApplyEventsOnInsert` | ❌ WRONG | Fix: `enableEditorFloorOptimization && incrementalFloorInsert && skipApplyEventsOnInsert` |

### Category 3: Wrong condition — Missing runtime reflection check

| # | Patch Type | Current Path | Current Condition | Old Condition | Status | Action Needed |
|---|---|---|---|---|---|---|
| 21 | `BugfixPatches.CoopPauseLockFixPlayerPatch` | `bugfix/coopPause` | `fixCoopPauseLock` | `fixCoopPauseLock && AccessTools.Method(typeof(scrPlayer), "LockInput") != null` | ❌ WRONG | Fix: add runtime reflection check for `scrPlayer.LockInput` |
| 22 | `BugfixPatches.CoopPauseLockFixControllerPatch` | `bugfix/coopPause` | `fixCoopPauseLock` | `fixCoopPauseLock && AccessTools.Method(typeof(scrController), "LockInput") != null` | ❌ WRONG | Fix: add runtime reflection check for `scrController.LockInput` |

### Category 4: Condition format difference (needs verification)

| # | Patch Type | Current Path | Current Condition | Old Condition | Status | Action Needed |
|---|---|---|---|---|---|---|
| 23 | `JsonPatches.LegacyBehaviorPatch` | `compatibility/legacyBehavior` | `legacyFlashMode,legacyCamRelativeToMode` | `legacyFlashMode != Default \|\| legacyCamRelativeToMode != Default` | ⚠️ VERIFY | Old was explicit `!= Default` OR; current is comma-separated string. Verify IriPatch evaluates enum fields correctly as `!= Default` |

### Category 5: Was not registered in old PatchManager (needs decision)

| # | Patch Type | Current Path | Current Condition | Old Condition | Status | Action Needed |
|---|---|---|---|---|---|---|
| 24 | `JudgeTextPatches.HitTextMeshShowRotationFixPatch` | `sound/judgeTextRotation` | `fixJudgeRotation` | NOT REGISTERED | ⚠️ NEW | Decision: patch is new, condition `fixJudgeRotation` appears intentional |
| 25 | `MiscPatches.CustomBpmPatch` | `ui/lobbyMusic` | `enableCustomBpm` | NOT REGISTERED | ⚠️ NEW | Decision: patch is new, condition `enableCustomBpm` appears intentional |

---

## Category 6: Correct (no change needed)

### Optimizer — root, texture, decor, scene, track, ffx, loading (non-spread), multithread

| # | Patch Type | Current Path | Current Condition | Old Condition | Status |
|---|---|---|---|---|---|
| 1 | `OptimizerPatches.TextureOptimizationPatch` | `optimizer/texture` | `enableOptimizer` | `enableOptimizer` | ✅ |
| 2 | `OptimizerPatches.OptimizationResetPatches` | `optimizer` | `enableOptimizer` | `enableOptimizer` | ✅ |
| 3 | `OptimizerPatches.BackgroundScalingPatch` | `optimizer/decor` | `enableOptimizer` | `enableOptimizer` | ✅ |
| 4 | `OptimizerPatches.MeshRendererScalingPatch` | `optimizer/decor` | `enableOptimizer` | `enableOptimizer` | ✅ |
| 5 | `OptimizerPatches.BorderScalingPatch` | `optimizer/decor` | `enableOptimizer` | `enableOptimizer` | ✅ |
| 6 | `OptimizerPatches.HitboxScalingPatch` | `optimizer/decor` | `enableOptimizer` | `enableOptimizer` | ✅ |
| 7 | `OptimizerPatches.WorldSizeScalingPatch` | `optimizer/decor` | `enableOptimizer` | `enableOptimizer` | ✅ |
| 8 | `OptimizerPatches.ShadowOptimizationPatch` | `optimizer` | `enableOptimizer` | `enableOptimizer` | ✅ |
| 9 | `OptimizerPatches.DecorationUpdateOptimizationPatch` | `optimizer/decor` | `enableOptimizer` | `enableOptimizer` | ✅ |
| 10 | `OptimizerPatches.TileUpdateOptimizationPatch` | `optimizer/scene` | `enableOptimizer` | `enableOptimizer` | ✅ |
| 11 | `OptimizerPatches.VisualDecorationUpdateHitboxPatch` | `optimizer/decor` | `enableOptimizer` | `enableOptimizer` | ✅ |
| 12 | `OptimizerPatches.ApplyEventsToFloorsOptimizationPatch` | `optimizer/scene` | `enableOptimizer` | `enableOptimizer` | ✅ |
| 13 | `OptimizerPatches.UpdateIconSpriteOptimizationPatch` | `optimizer/scene` | `enableOptimizer` | `enableOptimizer` | ✅ |
| 14 | `OptimizerPatches.MoveDecorationsOptimizationPatch` | `optimizer/decor` | `enableOptimizer` | `enableOptimizer` | ✅ |
| 15 | `OptimizerPatches.VRAMNotificationPatch` | `optimizer` | `enableOptimizer` | `enableOptimizer` | ✅ |
| 16 | `TrackOptimizationPatches.MoveFloorStartEffectPatch` | `optimizer/track` | `enableOptimizer` | `enableOptimizer` | ✅ |
| 17 | `TrackOptimizationPatches.RecolorFloorStartEffectPatch` | `optimizer/track` | `enableOptimizer` | `enableOptimizer` | ✅ |
| 18 | `FfxOptimizationPatches.OptimizeDecorationManagerLateUpdate` | `optimizer/ffx` | `enableOptimizer` | `enableOptimizer` | ✅ |

### Optimizer — scene (SceneOptimizationPatches)

| # | Patch Type | Current Path | Current Condition | Old Condition | Status |
|---|---|---|---|---|---|
| 19 | `SceneOptimizationPatches.ScnGameCacheReferencesPatch` | `optimizer/scene` | `enableOptimizer` | `enableOptimizer` | ✅ |
| 20 | `SceneOptimizationPatches.ScnGameUpdateOptimizationPatch` | `optimizer/scene` | `enableOptimizer` | `enableOptimizer` | ✅ |
| 21 | `SceneOptimizationPatches.ApplyEventsOptimizationPatch` | `optimizer/scene` | `enableOptimizer` | `enableOptimizer` | ✅ |
| 22 | `SceneOptimizationPatches.ScnGameCleanupPatch` | `optimizer/scene` | `enableOptimizer` | `enableOptimizer` | ✅ |
| 23 | `SceneOptimizationPatches.ObjectsAtMouseOptimizationPatch` | `optimizer/scene` | `enableOptimizer` | `enableOptimizer` | ✅ |
| 24 | `SceneOptimizationPatches.DestroyEventIndicatorsPatch` | `optimizer/scene` | `enableOptimizer` | `enableOptimizer` | ✅ |
| 25 | `SceneOptimizationPatches.ToggleFloorNumsActionPatch` | `optimizer/scene` | `enableOptimizer` | `enableOptimizer` | ✅ |

### Optimizer — loading (non-spread)

| # | Patch Type | Current Path | Current Condition | Old Condition | Status |
|---|---|---|---|---|---|
| 26 | `LoadingOptimizationPatches.EventPreprocessingPatch` | `optimizer/loading` | `enableOptimizer` | `enableOptimizer` | ✅ |
| 27 | `LoadingOptimizationPatches.LoadingOptimizationCleanupPatch` | `optimizer/loading` | `enableOptimizer` | `enableOptimizer` | ✅ |

### Optimizer — customEasing, eventTween, playerInput, rdInput, json, particle, multithread

| # | Patch Type | Current Path | Current Condition | Old Condition | Status |
|---|---|---|---|---|---|
| 28 | `CustomEasingPatches.MoveFloorPatch` | `optimizer/customEasing` | `enableCustomEasingEngine` | `enableCustomEasingEngine` | ✅ |
| 29 | `CustomEasingPatches.RecolorFloorPatch` | `optimizer/customEasing` | `enableCustomEasingEngine` | `enableCustomEasingEngine` | ✅ |
| 30 | `CustomEasingPatches.MoveDecorationPatch` | `optimizer/customEasing` | `enableCustomEasingEngine` | `enableCustomEasingEngine` | ✅ |
| 31 | `CustomEasingPatches.DotweenKillAllPatch` | `optimizer/customEasing` | `enableCustomEasingEngine` | `enableCustomEasingEngine` | ✅ |
| 32 | `EventTweenOptimizationPatches.FfxMoveFloorPlusEventTweensPatch` | `optimizer/eventTween` | `optimizeEventProcessing` | `optimizeEventProcessing` | ✅ |
| 33 | `EventTweenOptimizationPatches.FfxMoveDecorationsPlusEventTweensPatch` | `optimizer/eventTween` | `optimizeEventProcessing` | `optimizeEventProcessing` | ✅ |
| 34 | `EventTweenOptimizationPatches.FfxRecolorFloorPlusEventTweensPatch` | `optimizer/eventTween` | `optimizeEventProcessing` | `optimizeEventProcessing` | ✅ |
| 35 | `EventTweenOptimizationPatches.FfxPlusBaseKillCacheInvalidationPatch` | `optimizer/eventTween` | `optimizeEventProcessing` | `optimizeEventProcessing` | ✅ |
| 36 | `PlayerInputOptimizationPatches.SimulatedPlayerControlUpdatePatch` | `optimizer/playerInput` | `optimizePlayerInputAllocations` | `optimizePlayerInputAllocations` | ✅ |
| 37 | `RDInputOptimizationPatches.GetStateKeysPatch` | `optimizer/rdInput` | `optimizeRDInputAllocations` | `optimizeRDInputAllocations` | ✅ |
| 38 | `JsonPatches.PatchGetCustomLevelName` | `optimizer/json` | `customLevelReadOptimization` | `customLevelReadOptimization` | ✅ |
| 39 | `ParticleOptimizationPatches.ParticleCullingModePatch` | `optimizer/particle` | `optimizeParticle` | N/A (new patch) | ✅ |
| 40 | `ParticleOptimizationPatches.ParticleUpdateSkipPatch` | `optimizer/particle` | `optimizeParticle` | N/A (new patch) | ✅ |
| 41 | `ParticleOptimizationPatches.ParticlePoolOnClearPatch` | `optimizer/particle` | `optimizeParticle` | N/A (new patch) | ✅ |
| 42 | `ParticleOptimizationPatches.ParticlePoolOnCreatePatch` | `optimizer/particle` | `optimizeParticle` | N/A (new patch) | ✅ |
| 43 | `MultithreadOptimizationPatches.ParallelEventProcessingPatch` | `optimizer/multithread` | `enableOptimizer` | `enableOptimizer` | ✅ |

### AdaptivePatches

| # | Patch Type | Current Path | Current Condition | Old Condition | Status |
|---|---|---|---|---|---|
| 44 | `AdaptivePatches.TextureNameCleanup` | `optimizer/texture` | `enableOptimizer` | `enableOptimizer` | ✅ |
| 45 | `AdaptivePatches.DecorationScalingCustomSprite` | `optimizer/decor` | `enableOptimizer` | `enableOptimizer` | ✅ |
| 46 | `AdaptivePatches.DecorationScalingSprite` | `optimizer/decor` | `enableOptimizer` | `enableOptimizer` | ✅ |

### Bugfix patches

| # | Patch Type | Current Path | Current Condition | Old Condition | Status |
|---|---|---|---|---|---|
| 47 | `BugfixPatches.PortalTravelFixPatch` | `bugfix/portal` | `portalTravelFix` | `portalTravelFix` | ✅ |
| 48 | `BugfixPatches.AsyncInputPlaySnapPatch` | `bugfix` | AlwaysOn | AlwaysOn | ✅ |
| 49 | `BugfixPatches.EditorPlayResetMistakesPatch` | `bugfix/editorPlayReset` | `fixEditorPlayResetMistakes` | `fixEditorPlayResetMistakes` | ✅ |
| 50 | `BugfixPatches.TurnaroundConditionFix` | `bugfix/turnaround` | `fixTurnaroundCondition` | `fixTurnaroundCondition` | ✅ |
| 51 | `BugfixPatches.CoopPauseHandleLockFix` | `bugfix/coopPause` | `fixCoopPauseLock` | `fixCoopPauseLock` | ✅ |
| 52 | `BugfixPatches.CoopPlayerHitFix` | `bugfix/coopPause` | `fixCoopPauseLock` | `fixCoopPauseLock` | ✅ |

### Compatibility patches

| # | Patch Type | Current Path | Current Condition | Old Condition | Status |
|---|---|---|---|---|---|
| 53 | `CompatibilityPatches.LegacyPauseFixPatch_Play` | `compatibility/legacyPause` | `enableLegacyPauseFix` | `enableLegacyPauseFix` | ✅ |
| 54 | `CompatibilityPatches.NoFailTooEarlyPatch` | `compatibility/noFail` | `enableNoFailTooEarly` | `enableNoFailTooEarly` | ✅ |
| 55 | `CompatibilityPatches.ScaleFilterSpeedWithPitchPatch` | `compatibility/scaleFilter` | `scaleFilterSpeedWithPitch` | `scaleFilterSpeedWithPitch` | ✅ |
| 56 | `CameraRelativeDragPatches` | `compatibility/cameraDrag` | `fixCameraRelativeDrag` | `fixCameraRelativeDrag` | ✅ |
| 57 | `JsonPatches.ForceAngleDataPatch` | `compatibility/forceAngle` | `forceAngleData` | `forceAngleData` | ✅ |

### RequiredMods & CustomEvents

| # | Patch Type | Current Path | Current Condition | Old Condition | Status |
|---|---|---|---|---|---|
| 58 | `RequiredModsClearPatches.LevelDataClearPatch` | `compatibility/requiredMods` | `ignoreRequiredMods` | `ignoreRequiredMods` | ✅ |
| 59 | `RequiredModsClearPatches.LevelDataCLSClearPatch` | `compatibility/requiredMods` | `ignoreRequiredMods` | `ignoreRequiredMods` | ✅ |
| 60 | `RequiredModsClearPatches.EncodeRestorePatch` | `compatibility/requiredMods` | `ignoreRequiredMods` | `ignoreRequiredMods` | ✅ |
| 61 | `RequiredModsClearPatches.LevelLoadNotifyPatch` | `compatibility/requiredMods` | `ignoreRequiredMods` | `ignoreRequiredMods` | ✅ |
| 62–73 | `CustomEventsPatches.*` (12 patches) | `compatibility/customEvents` | `ignoreRequiredMods` | `ignoreRequiredMods` | ✅ |

### Sound / JudgeText

| # | Patch Type | Current Path | Current Condition | Old Condition | Status |
|---|---|---|---|---|---|
| 74 | `HitSoundPatch` | `sound/hitSound` | `enableHitSoundPitch` | `enableHitSoundPitch` | ✅ |
| 75 | `JudgeTextPatches.HitTextMeshInitPatch` | `sound/judgeText` | `enableJudgeTextCustomization` | `enableJudgeTextCustomization` | ✅ |
| 76 | `JudgeTextPatches.HitTextMeshShowPatch` | `sound/judgeText` | `enableJudgeTextCustomization` | `enableJudgeTextCustomization` | ✅ |
| 77 | `JudgeTextPatches.HitTextManagerShowPatch` | `sound/judgeTextRotation` | AlwaysOn | AlwaysOn | ✅ |

### Editor

| # | Patch Type | Current Path | Current Condition | Old Condition | Status |
|---|---|---|---|---|---|
| 78 | `EditorShortcutPatches.EditorShortcutUpdatePatch` | `editor/shortcuts` | `enableEditorShortcuts` | `enableEditorShortcuts` | ✅ |
| 79 | `EditorShortcutPatches.FloorSelectCameraJumpPatch` | `editor/shortcuts` | `enableEditorShortcuts` | `enableEditorShortcuts` | ✅ |
| 80 | `EditorPausePatches` | `ui/pause` | AlwaysOn | AlwaysOn | ✅ |

### UI / Misc

| # | Patch Type | Current Path | Current Condition | Old Condition | Status |
|---|---|---|---|---|---|
| 81 | `MiscPatches.RemoveNewsPatch` | `ui/news` | `removeNews` | `removeNews` | ✅ |
| 82 | `MiscPatches.HideBetaWatermarkPatch` | `ui/watermark` | `hideBetaWatermark` | `hideBetaWatermark` | ✅ |
| 83 | `MiscPatches.ForceDifficultyUIPatch` | `ui/difficulty` | `forceDifficultyUI` | `forceDifficultyUI` | ✅ |
| 84 | `MiscPatches.CircleArcPatch` | `ui/circleArc` | `enableCircleArc` | `enableCircleArc` | ✅ |
| 85 | `MiscPatches.AutoplayTextPositionPatch` | `ui/autoplayText` | `moveAutoplayText` | `moveAutoplayText` | ✅ |
| 86 | `MiscPatches.AlwaysCountdownPatch` | `ui/countdown` | `alwaysCountdown` | `alwaysCountdown` | ✅ |
| 87 | `MiscPatches.AutoplayHintUIPatch` | `ui/autoplayHint` | AlwaysOn | AlwaysOn | ✅ |
| 88 | `MiscPatches.LobbyMusicPatch` | `ui/lobbyMusic` | `enableLobbyMusicPatch` | `enableLobbyMusicPatch` | ✅ |
| 89 | `PausePlanetTrailPatch` | `ui/pauseTrail` | `enablePausePlanetTrail` | `enablePausePlanetTrail` | ✅ |

### AsyncInputOptimize (modules)

| # | Patch Type | Current Path | Current Condition | Old Condition | Status |
|---|---|---|---|---|---|
| 90 | `AsyncInputOptimize.Patch.__scnGame` | `asyncInput` | `enableAIO` | `enableAIO` | ✅ |
| 91 | `AsyncInputOptimize.Patch.__scrConductor` | `asyncInput` | `enableAIO` | `enableAIO` | ✅ |
| 92 | `AsyncInputOptimize.Patch.__scrCountdown` | `asyncInput` | `enableAIO` | `enableAIO` | ✅ |
| 93 | `AsyncInputOptimize.Patch.UnityEngine__SceneManagement__SceneManager` | `asyncInput` | `enableAIO` | `enableAIO` | ✅ |

---

## Summary

- **Total patches scanned**: 118
- **Correct (no change needed)**: 93
- **Wrong condition (critical)**: 22
- **Needs verification**: 1
- **New (not registered in old system)**: 2

### Breakdown of the 22 Wrong Conditions:

| Issue | Count | Patches |
|---|---|---|
| Missing `enableOptimizer` in compound condition | 10 | ExtremeOptimization (2), TweenSafety (4), FrameSpreadLoading (4) |
| EditorFloorOptimization: wrong base condition | 10 | All 10 EditorFloorOptimization patches |
| Missing runtime reflection check | 2 | CoopPauseLockFixPlayerPatch, CoopPauseLockFixControllerPatch |

### Priority Fix Order:
1. **ExtremeOptimizationPatches** (2) — add `enableOptimizer &&` prefix
2. **TweenSafetyPatches** (4) — add `enableOptimizer &&` prefix
3. **FrameSpreadDecorationLoadingPatch** (4) — add `enableOptimizer &&` prefix
4. **EditorFloorOptimizationPatches** (10) — rewrite all conditions to match old compound expressions
5. **CoopPauseLockFix** patches (2) — add runtime reflection check to condition
6. **LegacyBehaviorPatch** (1) — verify comma-separated condition works correctly for enum `!= Default` checks
