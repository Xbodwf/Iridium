# Patch Conditions Reference

Extracted from the OLD `PatchManager.cs` — every `PatchDef` registration with its exact `Func<bool>` condition.

---

## OptimizerPatches (14 nested types — RegisterNestedPatches)

All 14 nested types got: `enableOptimizer`
```csharp
var optCond = () => Main.Settings.optimizer.enableOptimizer;
RegisterNestedPatches(typeof(OptimizerPatches), optCond);
```

| Nested Type | Condition |
|---|---|
| TextureOptimizationPatch | enableOptimizer |
| OptimizationResetPatches | enableOptimizer |
| BackgroundScalingPatch | enableOptimizer |
| MeshRendererScalingPatch | enableOptimizer |
| BorderScalingPatch | enableOptimizer |
| HitboxScalingPatch | enableOptimizer |
| WorldSizeScalingPatch | enableOptimizer |
| ShadowOptimizationPatch | enableOptimizer |
| DecorationUpdateOptimizationPatch | enableOptimizer |
| TileUpdateOptimizationPatch | enableOptimizer |
| VisualDecorationUpdateHitboxPatch | enableOptimizer |
| ApplyEventsToFloorsOptimizationPatch | enableOptimizer |
| UpdateIconSpriteOptimizationPatch | enableOptimizer |
| MoveDecorationsOptimizationPatch | enableOptimizer |

---

## TrackOptimizationPatches (2 nested types — RegisterNestedPatches)

All 2 nested types got: `enableOptimizer`
```csharp
RegisterNestedPatches(typeof(TrackOptimizationPatches), optCond);
```

| Nested Type | Condition |
|---|---|
| MoveFloorStartEffectPatch | enableOptimizer |
| RecolorFloorStartEffectPatch | enableOptimizer |

Note: `CleanupPatch` exists in TrackOptimizationPatches.cs but has NO `[HarmonyPatch]` attribute and was NOT registered.

---

## FfxOptimizationPatches (1 nested type — RegisterNestedPatches)

All 1 nested type got: `enableOptimizer`
```csharp
RegisterNestedPatches(typeof(FfxOptimizationPatches), optCond);
```

| Nested Type | Condition |
|---|---|
| OptimizeDecorationManagerLateUpdate | enableOptimizer |

---

## SceneOptimizationPatches (6 nested types — RegisterNestedPatches)

All 6 nested types got: `enableOptimizer`
```csharp
RegisterNestedPatches(typeof(SceneOptimizationPatches), optCond);
```

| Nested Type | Condition |
|---|---|
| ScnGameCacheReferencesPatch | enableOptimizer |
| ScnGameUpdateOptimizationPatch | enableOptimizer |
| ApplyEventsOptimizationPatch | enableOptimizer |
| ScnGameCleanupPatch | enableOptimizer |
| ObjectsAtMouseOptimizationPatch | enableOptimizer |
| DestroyEventIndicatorsPatch | enableOptimizer |
| ToggleFloorNumsActionPatch | enableOptimizer |

Note: `EditorUpdateScrollRectCachePatch` has NO `[HarmonyPatch]` attribute — it's a utility class, not a registered patch.

---

## LoadingOptimizationPatches (3 nested types — RegisterNestedPatches, minus FrameSpreadDecorationLoadingPatch)

```csharp
RegisterNestedPatches(typeof(LoadingOptimizationPatches), optCond,
    new HashSet<Type> { typeof(LoadingOptimizationPatches.FrameSpreadDecorationLoadingPatch) });
```

| Nested Type | Condition |
|---|---|
| EventPreprocessingPatch | enableOptimizer |
| LoadingOptimizationCleanupPatch | enableOptimizer |

`FrameSpreadDecorationLoadingPatch` was EXCLUDED from RegisterNestedPatches and registered individually (see below).

---

## FrameSpreadDecorationLoadingPatch (individual registrations)

All 4 have the SAME condition: `enableOptimizer && frameSpreadDecorationLoading`
```csharp
_definitions.Add(new PatchDef(typeof(LoadingOptimizationPatches.FrameSpreadDecorationLoadingPatch),
    () => Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.frameSpreadDecorationLoading));
_definitions.Add(new PatchDef(typeof(LoadingOptimizationPatches.FrameSpreadDecorationLoadingPatch.ReloadAssets_Patch),
    () => Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.frameSpreadDecorationLoading));
_definitions.Add(new PatchDef(typeof(LoadingOptimizationPatches.FrameSpreadDecorationLoadingPatch.ResetDecorations_Patch),
    () => Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.frameSpreadDecorationLoading));
_definitions.Add(new PatchDef(typeof(LoadingOptimizationPatches.FrameSpreadDecorationLoadingPatch.Play_Patch),
    () => Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.frameSpreadDecorationLoading));
```

| Nested Type | Condition |
|---|---|
| FrameSpreadDecorationLoadingPatch | enableOptimizer && frameSpreadDecorationLoading |
| FrameSpreadDecorationLoadingPatch.ReloadAssets_Patch | enableOptimizer && frameSpreadDecorationLoading |
| FrameSpreadDecorationLoadingPatch.ResetDecorations_Patch | enableOptimizer && frameSpreadDecorationLoading |
| FrameSpreadDecorationLoadingPatch.Play_Patch | enableOptimizer && frameSpreadDecorationLoading |

---

## ExtremeOptimizationPatches (2 nested types — RegisterNestedPatches)

All 2 nested types got: `enableOptimizer && enableExtremeOptimization`
```csharp
RegisterNestedPatches(typeof(ExtremeOptimizationPatches),
    () => Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.enableExtremeOptimization);
```

| Nested Type | Condition |
|---|---|
| ExtremeMoveFloorPatch | enableOptimizer && enableExtremeOptimization |
| ExtremeMoveDecorPatch | enableOptimizer && enableExtremeOptimization |

---

## TweenSafetyPatches (4 nested types — RegisterNestedPatches)

All 4 nested types got: `enableOptimizer && dotweenDefaultRecyclable`
```csharp
var tweenSafetyCond = () => Main.Settings.optimizer.enableOptimizer && Main.Settings.optimizer.dotweenDefaultRecyclable;
RegisterNestedPatches(typeof(TweenSafetyPatches), tweenSafetyCond);
```

| Nested Type | Condition |
|---|---|
| ClearPausedTweensOnReset | enableOptimizer && dotweenDefaultRecyclable |
| SafeDecorationOnDestroy | enableOptimizer && dotweenDefaultRecyclable |
| SafeFfxPlusBaseKill | enableOptimizer && dotweenDefaultRecyclable |
| SafeFfxPlusBaseScrubToTime | enableOptimizer && dotweenDefaultRecyclable |

---

## EventTweenOptimizationPatches (4 individual PatchDef registrations)

All 4 have the SAME condition: `optimizeEventProcessing`
```csharp
var eventTweenCond = () => Main.Settings.optimizer.optimizeEventProcessing;
```

| Type | Condition |
|---|---|
| EventTweenOptimizationPatches.FfxMoveFloorPlusEventTweensPatch | optimizeEventProcessing |
| EventTweenOptimizationPatches.FfxMoveDecorationsPlusEventTweensPatch | optimizeEventProcessing |
| EventTweenOptimizationPatches.FfxRecolorFloorPlusEventTweensPatch | optimizeEventProcessing |
| EventTweenOptimizationPatches.FfxPlusBaseKillCacheInvalidationPatch | optimizeEventProcessing |

---

## EditorFloorOptimizationPatches (10 individual PatchDef registrations)

Master condition: `enableEditorFloorOptimization` (stored in `editorMaster` lambda)

| Type | Condition |
|---|---|
| EditorFloorOptimizationPatches.InsertCharFloorOptimizationPatch | enableEditorFloorOptimization && incrementalFloorInsert |
| EditorFloorOptimizationPatches.InsertFloatFloorOptimizationPatch | enableEditorFloorOptimization && incrementalFloorInsert |
| EditorFloorOptimizationPatches.InstantiateFloatFloorsOptimizationPatch | enableEditorFloorOptimization && incrementalFloorInsert |
| EditorFloorOptimizationPatches.DeleteFloorOptimizationPatch | enableEditorFloorOptimization && incrementalFloorInsert |
| EditorFloorOptimizationPatches.RemakePathRedundancyPatch | enableEditorFloorOptimization && incrementalFloorInsert && skipRedundantRemakePath |
| EditorFloorOptimizationPatches.GameRemakePathOptimizationPatch | enableEditorFloorOptimization && incrementalFloorInsert && skipRedundantRemakePath |
| EditorFloorOptimizationPatches.DrawFloorNumsOptimizationPatch | enableEditorFloorOptimization && incrementalFloorInsert && rangeBasedRedraw |
| EditorFloorOptimizationPatches.DrawFloorOffsetLinesOptimizationPatch | enableEditorFloorOptimization && incrementalFloorInsert && skipRedundantRemakePath |
| EditorFloorOptimizationPatches.OffsetFloorIDsOptimizationPatch | enableEditorFloorOptimization && incrementalFloorInsert && optimizeOffsetFloorEvents |
| EditorFloorOptimizationPatches.SkipApplyEventsOnInsertPatch | enableEditorFloorOptimization && incrementalFloorInsert && skipApplyEventsOnInsert |

---

## PlayerInputOptimizationPatches (1 individual PatchDef registration)

```csharp
_definitions.Add(new PatchDef(typeof(PlayerInputOptimizationPatches.SimulatedPlayerControlUpdatePatch),
    () => Main.Settings.optimizer.optimizePlayerInputAllocations));
```

| Type | Condition |
|---|---|
| PlayerInputOptimizationPatches.SimulatedPlayerControlUpdatePatch | optimizePlayerInputAllocations |

---

## RDInputOptimizationPatches (1 individual PatchDef registration)

```csharp
_definitions.Add(new PatchDef(typeof(RDInputOptimizationPatches.GetStateKeysPatch),
    () => Main.Settings.optimizer.optimizeRDInputAllocations));
```

| Type | Condition |
|---|---|
| RDInputOptimizationPatches.GetStateKeysPatch | optimizeRDInputAllocations |

---

## CustomEasingPatches (4 nested types — RegisterNestedPatches)

All 4 nested types got: `enableCustomEasingEngine`
```csharp
var easingCond = () => Main.Settings.optimizer.enableCustomEasingEngine;
RegisterNestedPatches(typeof(CustomEasingPatches), easingCond);
```

| Nested Type | Condition |
|---|---|
| MoveFloorPatch | enableCustomEasingEngine |
| RecolorFloorPatch | enableCustomEasingEngine |
| MoveDecorationPatch | enableCustomEasingEngine |
| DotweenKillAllPatch | enableCustomEasingEngine |

---

## JsonPatches (3 individual PatchDef registrations)

| Type | Condition |
|---|---|
| JsonPatches.PatchGetCustomLevelName | customLevelReadOptimization |
| JsonPatches.ForceAngleDataPatch | forceAngleData |
| JsonPatches.LegacyBehaviorPatch | legacyFlashMode != Default \|\| legacyCamRelativeToMode != Default |

LegacyBehaviorPatch condition (exact expression):
```csharp
() => Main.Settings.compatibility.legacyFlashMode != LegacyBehaviorMode.Default ||
      Main.Settings.compatibility.legacyCamRelativeToMode != LegacyBehaviorMode.Default
```

---

## BugfixPatches (7 individual PatchDef registrations)

| Type | Condition |
|---|---|
| BugfixPatches.PortalTravelFixPatch | portalTravelFix |
| BugfixPatches.AsyncInputPlaySnapPatch | AlwaysOn |
| BugfixPatches.EditorPlayResetMistakesPatch | fixEditorPlayResetMistakes |
| BugfixPatches.TurnaroundConditionFix | fixTurnaroundCondition |
| BugfixPatches.CoopPauseHandleLockFix | fixCoopPauseLock |
| BugfixPatches.CoopPlayerHitFix | fixCoopPauseLock |
| BugfixPatches.CoopPauseLockFixPlayerPatch | fixCoopPauseLock && AccessTools.Method(typeof(scrPlayer), "LockInput") != null |
| BugfixPatches.CoopPauseLockFixControllerPatch | fixCoopPauseLock && AccessTools.Method(typeof(scrController), "LockInput") != null |

Note: `CoopPauseLockFix` and `CoopPlayerHitFix` have the simple condition `fixCoopPauseLock`.
`CoopPauseLockFixPlayerPatch` and `CoopPauseLockFixControllerPatch` have compound conditions with runtime reflection checks.

---

## MiscPatches (9 individual PatchDef registrations)

| Type | Condition |
|---|---|
| MiscPatches.RemoveNewsPatch | removeNews |
| MiscPatches.HideBetaWatermarkPatch | hideBetaWatermark |
| MiscPatches.ForceDifficultyUIPatch | forceDifficultyUI |
| MiscPatches.CircleArcPatch | enableCircleArc |
| MiscPatches.AutoplayTextPositionPatch | moveAutoplayText |
| MiscPatches.AlwaysCountdownPatch | alwaysCountdown |
| MiscPatches.AutoplayHintUIPatch | AlwaysOn |
| MiscPatches.LobbyMusicPatch | enableLobbyMusicPatch |
| MiscPatches.CustomBpmPatch | NOT REGISTERED (exists in file but not in old PatchManager) |

---

## PausePlanetTrailPatch (1 individual PatchDef registration)

```csharp
_definitions.Add(new PatchDef(typeof(PausePlanetTrailPatch), () => Main.Settings.ui.enablePausePlanetTrail));
```

| Type | Condition |
|---|---|
| PausePlanetTrailPatch | enablePausePlanetTrail |

---

## CompatibilityPatches (3 individual PatchDef registrations)

```csharp
var pauseFixCond = () => Main.Settings.compatibility.enableLegacyPauseFix;
```

| Type | Condition |
|---|---|
| CompatibilityPatches.LegacyPauseFixPatch_Play | enableLegacyPauseFix |
| CompatibilityPatches.NoFailTooEarlyPatch | enableNoFailTooEarly |
| CompatibilityPatches.ScaleFilterSpeedWithPitchPatch | scaleFilterSpeedWithPitch |

---

## CameraRelativeDragPatches (1 individual PatchDef registration)

```csharp
_definitions.Add(new PatchDef(typeof(CameraRelativeDragPatches), () => Main.Settings.compatibility.fixCameraRelativeDrag));
```

| Type | Condition |
|---|---|
| CameraRelativeDragPatches | fixCameraRelativeDrag |

---

## RequiredModsClearPatches (4 individual PatchDef registrations)

All 4 have the SAME condition: `ignoreRequiredMods`
```csharp
var requiredModsCond = () => Main.Settings.compatibility.ignoreRequiredMods;
```

| Type | Condition |
|---|---|
| RequiredModsClearPatches.LevelDataClearPatch | ignoreRequiredMods |
| RequiredModsClearPatches.LevelDataCLSClearPatch | ignoreRequiredMods |
| RequiredModsClearPatches.EncodeRestorePatch | ignoreRequiredMods |
| RequiredModsClearPatches.LevelLoadNotifyPatch | ignoreRequiredMods |

---

## CustomEventsPatches (12 individual PatchDef registrations)

All 12 have the SAME condition: `ignoreRequiredMods`
```csharp
var customEventsCond = () => Main.Settings.compatibility.ignoreRequiredMods;
```

| Type | Condition |
|---|---|
| CustomEventsPatches.ScanRegisterPatch | ignoreRequiredMods |
| CustomEventsPatches.ScanRegisterCLSPatch | ignoreRequiredMods |
| CustomEventsPatches.FakeEventDecodePatch | ignoreRequiredMods |
| CustomEventsPatches.FakeEventEncodePatch | ignoreRequiredMods |
| CustomEventsPatches.ReadOnlyPanelPatch | ignoreRequiredMods |
| CustomEventsPatches.ListItemEventPatch | ignoreRequiredMods |
| CustomEventsPatches.EventIndicatorPatch | ignoreRequiredMods |
| CustomEventsPatches.ShowPanelFakeEventPatch | ignoreRequiredMods |
| CustomEventsPatches.RemoveEventAtSelectedPatch | ignoreRequiredMods |
| CustomEventsPatches.ShowTabsForFloorPatch | ignoreRequiredMods |
| CustomEventsPatches.FakeTabSetSelectedPatch | ignoreRequiredMods |
| CustomEventsPatches.FakeTabClickPatch | ignoreRequiredMods |

---

## HitSoundPatch (1 individual PatchDef registration)

```csharp
_definitions.Add(new PatchDef(typeof(HitSoundPatch), () => Main.Settings.hitSound.enableHitSoundPitch));
```

| Type | Condition |
|---|---|
| HitSoundPatch | enableHitSoundPitch |

---

## JudgeTextPatches (3 individual PatchDef registrations)

```csharp
_definitions.Add(new PatchDef(typeof(JudgeTextPatches.HitTextMeshInitPatch), () => Main.Settings.judgeText.enableJudgeTextCustomization));
_definitions.Add(new PatchDef(typeof(JudgeTextPatches.HitTextManagerShowPatch), () => true));
_definitions.Add(new PatchDef(typeof(JudgeTextPatches.HitTextMeshShowPatch), () => Main.Settings.judgeText.enableJudgeTextCustomization));
```

| Type | Condition |
|---|---|
| JudgeTextPatches.HitTextMeshInitPatch | enableJudgeTextCustomization |
| JudgeTextPatches.HitTextManagerShowPatch | AlwaysOn |
| JudgeTextPatches.HitTextMeshShowPatch | enableJudgeTextCustomization |

Note: `JudgeTextPatches.HitTextMeshShowRotationFixPatch` is NOT registered in the old PatchManager. It exists in the file with `[IriPatch]` attribute but has no `PatchDef` registration.

---

## EditorShortcutPatches (2 individual PatchDef registrations)

```csharp
_definitions.Add(new PatchDef(typeof(EditorShortcutPatches.EditorShortcutUpdatePatch), () => Main.Settings.editorShortcuts.enableEditorShortcuts));
_definitions.Add(new PatchDef(typeof(EditorShortcutPatches.FloorSelectCameraJumpPatch), () => Main.Settings.editorShortcuts.enableEditorShortcuts));
```

| Type | Condition |
|---|---|
| EditorShortcutPatches.EditorShortcutUpdatePatch | enableEditorShortcuts |
| EditorShortcutPatches.FloorSelectCameraJumpPatch | enableEditorShortcuts |

---

## EditorPausePatches (1 individual PatchDef registration)

```csharp
_definitions.Add(new PatchDef(typeof(EditorPausePatches), () => true));
```

| Type | Condition |
|---|---|
| EditorPausePatches | AlwaysOn |

---

## AdaptivePatches (3 individual RegisterPatch calls)

All 3 have the SAME condition: `enableOptimizer`
```csharp
RegisterPatch("TextureNameCleanup", new AdaptivePatches.TextureNameCleanup(), optCond);
RegisterPatch("DecorationScalingCustomSprite", new AdaptivePatches.DecorationScalingCustomSprite(), optCond);
RegisterPatch("DecorationScalingSprite", new AdaptivePatches.DecorationScalingSprite(), optCond);
```

| Type | Condition |
|---|---|
| AdaptivePatches.TextureNameCleanup | enableOptimizer |
| AdaptivePatches.DecorationScalingCustomSprite | enableOptimizer |
| AdaptivePatches.DecorationScalingSprite | enableOptimizer |

---

## AsyncInputOptimize (4 individual PatchDef registrations)

All 4 have the SAME condition: `enableAIO`
```csharp
_definitions.Add(new PatchDef(typeof(Iridium.Modules.AsyncInputOptimize.Patch.UnityEngine__SceneManagement__SceneManager), () => Iridium.Main.Settings.asyncInput.enableAIO));
_definitions.Add(new PatchDef(typeof(Iridium.Modules.AsyncInputOptimize.Patch.__scnGame), () => Iridium.Main.Settings.asyncInput.enableAIO));
_definitions.Add(new PatchDef(typeof(Iridium.Modules.AsyncInputOptimize.Patch.__scrConductor), () => Iridium.Main.Settings.asyncInput.enableAIO));
_definitions.Add(new PatchDef(typeof(Iridium.Modules.AsyncInputOptimize.Patch.__scrCountdown), () => Iridium.Main.Settings.asyncInput.enableAIO));
```

| Type | Condition |
|---|---|
| AsyncInputOptimize.Patch.UnityEngine__SceneManagement__SceneManager | enableAIO |
| AsyncInputOptimize.Patch.__scnGame | enableAIO |
| AsyncInputOptimize.Patch.__scrConductor | enableAIO |
| AsyncInputOptimize.Patch.__scrCountdown | enableAIO |

---

## Summary: Total Patch Count

| Registration Method | Count |
|---|---|
| RegisterNestedPatches (OptimizerPatches) | 14 |
| RegisterNestedPatches (TrackOptimizationPatches) | 2 |
| RegisterNestedPatches (FfxOptimizationPatches) | 1 |
| RegisterNestedPatches (SceneOptimizationPatches) | 6 |
| RegisterNestedPatches (LoadingOptimizationPatches, minus excluded) | 2 |
| RegisterNestedPatches (ExtremeOptimizationPatches) | 2 |
| RegisterNestedPatches (TweenSafetyPatches) | 4 |
| RegisterNestedPatches (CustomEasingPatches) | 4 |
| Individual PatchDef: EditorFloorOptimizationPatches | 10 |
| Individual PatchDef: FrameSpreadDecorationLoadingPatch + nested | 4 |
| Individual PatchDef: EventTweenOptimizationPatches | 4 |
| Individual PatchDef: PlayerInputOptimizationPatches | 1 |
| Individual PatchDef: RDInputOptimizationPatches | 1 |
| Individual PatchDef: JsonPatches | 3 |
| Individual PatchDef: BugfixPatches | 8 |
| Individual PatchDef: MiscPatches | 8 |
| Individual PatchDef: PausePlanetTrailPatch | 1 |
| Individual PatchDef: CompatibilityPatches | 3 |
| Individual PatchDef: CameraRelativeDragPatches | 1 |
| Individual PatchDef: RequiredModsClearPatches | 4 |
| Individual PatchDef: CustomEventsPatches | 12 |
| Individual PatchDef: HitSoundPatch | 1 |
| Individual PatchDef: JudgeTextPatches | 3 |
| Individual PatchDef: EditorShortcutPatches | 2 |
| Individual PatchDef: EditorPausePatches | 1 |
| RegisterPatch: AdaptivePatches | 3 |
| Individual PatchDef: AsyncInputOptimize | 4 |
| **TOTAL** | **~110** |
