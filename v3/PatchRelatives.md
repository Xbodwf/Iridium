# Patch Relative Analysis

## Old PatchManager → New IriPatch Path Mapping

### Legend
- **Condition** = the setting that must be `true` for the patch to be active
- **Old Path** = the group the patch was in under the OLD PatchManager (all OptimizerPatches got `enableOptimizer` via `RegisterNestedPatches`)
- **Correct Path** = the logically correct sub-path based on what the patch actually targets

---

## OPTIMIZER PATCHES

### optimizer (root) — Condition: `enableOptimizer`
General optimizer patches not specific to any sub-group.

| Class | Target | Description | Correct Path |
|---|---|---|---|
| `OptimizationResetPatches` | `scnGame` (OnDestroy, LoadAndPlayLevel, LoadLevel, Awake) | Resets decor optimization state on scene changes; cleans up track cache | `optimizer` |
| `ShadowOptimizationPatch` | `scnGame.LoadLevel` | Disables shadows when `disableShadows` is set | `optimizer` |
| `VRAMNotificationPatch` | `scnGame.UpdateDecorationObjects` | Shows VRAM saved notification after decoration reload | `optimizer` |

### optimizer/texture — Condition: `enableOptimizer`
Texture loading, resizing, compression, and VRAM tracking.

| Class | Target | Description | Correct Path |
|---|---|---|---|
| `OptimizerPatches.TextureOptimizationPatch` | `TextureManager.LoadTexture` | Pre-compresses and resizes textures on load; tracks decor ratios and VRAM savings | `optimizer/texture` |
| `AdaptivePatches.TextureNameCleanup` | `TextureManager.ApplyOptionsToTexture` or `LoadTexture` | Strips "(Clone)" suffix from texture names | `optimizer/texture` |

### optimizer/decor — Condition: `enableOptimizer`
Decoration rendering, scaling, hitbox, border, and sprite management.

| Class | Target | Description | Correct Path |
|---|---|---|---|
| `OptimizerPatches.BackgroundScalingPatch` | `scrCustomBackgroundSprite.SetCustomBG` | Scales background sprite size based on texture resize ratio | `optimizer/decor` |
| `OptimizerPatches.MeshRendererScalingPatch` | `scrVisualDecoration.UpdateShader` | Scales mesh renderer transform for resized textures | `optimizer/decor` |
| `OptimizerPatches.BorderScalingPatch` | `scrDecorationManager.UpdateBordersSizes` | Scales decoration border sizes by resize ratio | `optimizer/decor` |
| `OptimizerPatches.HitboxScalingPatch` | `scrDecorationManager.UpdateHitboxSizes` | Scales decoration hitbox sizes by resize ratio | `optimizer/decor` |
| `OptimizerPatches.WorldSizeScalingPatch` | `scrVisualDecoration.GetDecorationWorldSize` | Scales world size calculation by resize ratio | `optimizer/decor` |
| `OptimizerPatches.DecorationUpdateOptimizationPatch` | `scrVisualDecoration.Awake` | Disables shadows/light probes on decoration sprite renderers | `optimizer/decor` |
| `OptimizerPatches.VisualDecorationUpdateHitboxPatch` | `scrVisualDecoration.UpdateHitbox` | Scales damage hitbox by sprite scale; disables hitbox when `fastLoading` | `optimizer/decor` |
| `OptimizerPatches.MoveDecorationsOptimizationPatch` | `ffxMoveDecorationsPlus.StartEffect` | Optimized MoveDecoration implementation (bypasses DOTween for simple cases) | `optimizer/decor` |
| `AdaptivePatches.DecorationScalingCustomSprite` | `scrVisualDecoration.SetSprite(CustomSprite)` | Applies decor ratio scaling when CustomSprite is set | `optimizer/decor` |
| `AdaptivePatches.DecorationScalingSprite` | `scrVisualDecoration.SetSprite(Sprite)` | Applies decor ratio scaling when Sprite is set | `optimizer/decor` |

### optimizer/scene — Condition: `enableOptimizer`
Game scene, floor updates, gameplay, and editor scene optimizations.

| Class | Target | Description | Correct Path |
|---|---|---|---|
| `OptimizerPatches.ApplyEventsToFloorsOptimizationPatch` | `scnGame.ApplyEventsToFloors` | Skips event application when paused | `optimizer/scene` |
| `OptimizerPatches.TileUpdateOptimizationPatch` | `scrFloor.Awake` | Disables shadows/light probes on floor tile mesh renderers | `optimizer/scene` |
| `OptimizerPatches.UpdateIconSpriteOptimizationPatch` | `scrFloor.UpdateIconSprite` | Skips icon sprite update on non-main thread when `optimizeEventIcons` | `optimizer/scene` |

### optimizer/track — Condition: `enableOptimizer`
Track (floor) movement and recoloring optimizations.

| Class | Target | Description | Correct Path |
|---|---|---|---|
| `TrackOptimizationPatches.MoveFloorStartEffectPatch` | `ffxMoveFloorPlus.StartEffect` | Optimized MoveTrack implementation with cached transforms | `optimizer/track` |
| `TrackOptimizationPatches.RecolorFloorStartEffectPatch` | `ffxRecolorFloorPlus.StartEffect` | Optimized RecolorTrack implementation | `optimizer/track` |

### optimizer/ffx — Condition: `enableOptimizer`
Decoration manager LateUpdate and FFX loop optimization.

| Class | Target | Description | Correct Path |
|---|---|---|---|
| `FfxOptimizationPatches.OptimizeDecorationManagerLateUpdate` | `scrDecorationManager.LateUpdate` | Replaces LateUpdate to only update visible/tweening decorations | `optimizer/ffx` |

### optimizer/loading — Condition: `enableOptimizer`
Loading optimization, event preprocessing, frame-spread decoration loading.

| Class | Target | Description | Correct Path |
|---|---|---|---|
| `LoadingOptimizationPatches.EventPreprocessingPatch` | `scnGame.ApplyEventsToFloors` | Pre-classifies events by floor for faster lookup | `optimizer/loading` |
| `LoadingOptimizationPatches.FrameSpreadDecorationLoadingPatch` | `scnGame.UpdateDecorationObjects` | Spreads decoration loading across frames to prevent freezes | `optimizer/loading` |
| `LoadingOptimizationPatches.FrameSpreadDecorationLoadingPatch.ReloadAssets_Patch` | `scnGame.ReloadAssets` | Flags level-load for frame-spread interception | `optimizer/loading` |
| `LoadingOptimizationPatches.FrameSpreadDecorationLoadingPatch.ResetDecorations_Patch` | `scrDecorationManager.ResetDecorations` | Blocks ResetDecorations during frame-spread load | `optimizer/loading` |
| `LoadingOptimizationPatches.FrameSpreadDecorationLoadingPatch.Play_Patch` | `scnGame.Play` | Blocks Play during frame-spread load | `optimizer/loading` |
| `LoadingOptimizationPatches.LoadingOptimizationCleanupPatch` | `scnGame.OnDestroy` | Cleans up caches and tween pool on scene destroy | `optimizer/loading` |

### optimizer/extreme — Condition: `enableOptimizer && enableExtremeOptimization`
Extreme optimization for large batch effects.

| Class | Target | Description | Correct Path |
|---|---|---|---|
| `ExtremeOptimizationPatches.ExtremeMoveFloorPatch` | `ffxMoveFloorPlus.StartEffect` | Batched MoveTrack for 50+ floors with frame spreading | `optimizer/extreme` |
| `ExtremeOptimizationPatches.ExtremeMoveDecorPatch` | `ffxMoveDecorationsPlus.StartEffect` | Batched MoveDecoration for 50+ decorations with frame spreading | `optimizer/extreme` |

### optimizer/tweenSafety — Condition: `enableOptimizer && dotweenDefaultRecyclable`
DOTween recyclable safety patches to prevent stale tween references.

| Class | Target | Description | Correct Path |
|---|---|---|---|
| `TweenSafetyPatches.ClearPausedTweensOnReset` | `scrVfxPlus.Reset` | Clears pausedTweens list to prevent stale references across levels | `optimizer/tweenSafety` |
| `TweenSafetyPatches.SafeDecorationOnDestroy` | `scrDecoration.OnDestroy` | Safe Kill with IsActive() check on eventTweens | `optimizer/tweenSafety` |
| `TweenSafetyPatches.SafeFfxPlusBaseKill` | `ffxPlusBase.Kill` | Safe Kill with null/IsActive check on eventTweens | `optimizer/tweenSafety` |
| `TweenSafetyPatches.SafeFfxPlusBaseScrubToTime` | `ffxPlusBase.ScrubToTime` | Safe Goto/Kill with IsActive checks; prevents stale pausedTweens accumulation | `optimizer/tweenSafety` |

### optimizer/customEasing — Condition: `enableCustomEasingEngine`
Custom easing engine replacing DOTween for event tweens.

| Class | Target | Description | Correct Path |
|---|---|---|---|
| `CustomEasingPatches.MoveFloorPatch` | `ffxMoveFloorPlus.StartEffect` | Custom easing MoveTrack (replaces DOTween) | `optimizer/customEasing` |
| `CustomEasingPatches.RecolorFloorPatch` | `ffxRecolorFloorPlus.StartEffect` | Custom easing RecolorTrack (replaces DOTween) | `optimizer/customEasing` |
| `CustomEasingPatches.MoveDecorationPatch` | `ffxMoveDecorationsPlus.StartEffect` | Custom easing MoveDecoration (replaces DOTween) | `optimizer/customEasing` |
| `CustomEasingPatches.DotweenKillAllPatch` | `DOTween.KillAll` | Syncs CustomEasingEngine cleanup when DOTween.KillAll fires | `optimizer/customEasing` |

### optimizer/eventTween — Condition: `optimizeEventProcessing`
Event tween list caching to avoid repeated LINQ allocations.

| Class | Target | Description | Correct Path |
|---|---|---|---|
| `EventTweenOptimizationPatches.FfxMoveFloorPlusEventTweensPatch` | `ffxMoveFloorPlus.eventTweens` (getter) | Caches eventTweens list for MoveFloor | `optimizer/eventTween` |
| `EventTweenOptimizationPatches.FfxMoveDecorationsPlusEventTweensPatch` | `ffxMoveDecorationsPlus.eventTweens` (getter) | Caches eventTweens list for MoveDecorations | `optimizer/eventTween` |
| `EventTweenOptimizationPatches.FfxRecolorFloorPlusEventTweensPatch` | `ffxRecolorFloorPlus.eventTweens` (getter) | Caches eventTweens list for RecolorFloor | `optimizer/eventTween` |
| `EventTweenOptimizationPatches.FfxPlusBaseKillCacheInvalidationPatch` | `ffxPlusBase.Kill` | Invalidates eventTweens cache on Kill | `optimizer/eventTween` |

### optimizer/editorFloor — Condition: `enableEditorFloorOptimization` (with sub-conditions)
Editor floor insert/delete/draw optimizations for large levels.

| Class | Target | Description | Correct Path |
|---|---|---|---|
| `EditorFloorOptimizationPatches.InsertCharFloorOptimizationPatch` | `scnEditor.InsertCharFloor` | Incremental char floor insert (requires `incrementalFloorInsert`) | `optimizer/editorFloor/insert` |
| `EditorFloorOptimizationPatches.InsertFloatFloorOptimizationPatch` | `scnEditor.InsertFloatFloor` | Incremental float floor insert (requires `incrementalFloorInsert`) | `optimizer/editorFloor/insert` |
| `EditorFloorOptimizationPatches.InstantiateFloatFloorsOptimizationPatch` | `scrLevelMaker.InstantiateFloatFloors` | Reuses existing floor GameObjects in incremental mode | `optimizer/editorFloor/insert` |
| `EditorFloorOptimizationPatches.DeleteFloorOptimizationPatch` | `scnEditor.DeleteFloor` | Incremental floor delete via Transpiler | `optimizer/editorFloor/insert` |
| `EditorFloorOptimizationPatches.RemakePathRedundancyPatch` | `scnEditor.RemakePath(bool, bool)` | Skips redundant RemakePath for visual-only refreshes | `optimizer/editorFloor/insert` |
| `EditorFloorOptimizationPatches.GameRemakePathOptimizationPatch` | `scnGame.RemakePath(bool, bool)` | Skips game-level RemakePath for visual-only editor calls | `optimizer/editorFloor/insert` |
| `EditorFloorOptimizationPatches.DrawFloorNumsOptimizationPatch` | `scnEditor.DrawFloorNums` | Optimized floor number visibility toggle | `optimizer/editorFloor/insert` |
| `EditorFloorOptimizationPatches.DrawFloorOffsetLinesOptimizationPatch` | `scnEditor.DrawFloorOffsetLines` | Skips offset line drawing when no PositionTrack events | `optimizer/editorFloor/insert` |
| `EditorFloorOptimizationPatches.OffsetFloorIDsOptimizationPatch` | `scnEditor.OffsetFloorIDsInEvents` | Optimized floor ID offset for events/decorations | `optimizer/editorFloor/insert` |
| `EditorFloorOptimizationPatches.SkipApplyEventsOnInsertPatch` | `scnGame.ApplyEventsToFloors` | Skips full event re-application during incremental insert | `optimizer/editorFloor/insert` |

### optimizer/playerInput — Condition: `optimizePlayerInputAllocations`
Eliminates per-frame lambda allocations in player input hot path.

| Class | Target | Description | Correct Path |
|---|---|---|---|
| `PlayerInputOptimizationPatches.SimulatedPlayerControlUpdatePatch` | `scrPlayer.Simulated_PlayerControl_Update` | Replaces per-frame lambda allocations with compiled delegates | `optimizer/playerInput` |

### optimizer/rdInput — Condition: `optimizeRDInputAllocations`
Eliminates per-frame List allocations in RDInput queries.

| Class | Target | Description | Correct Path |
|---|---|---|---|
| `RDInputOptimizationPatches.GetStateKeysPatch` | `RDInput.GetStateKeys` | Pooled List<AnyKeyCode> reuse to avoid per-frame allocations | `optimizer/rdInput` |

### optimizer/json — Condition: `customLevelReadOptimization`
JSON deserialization optimization for custom level loading.

| Class | Target | Description | Correct Path |
|---|---|---|---|
| `JsonPatches.PatchGetCustomLevelName` | `LevelData.GetCustomLevelName` | Uses DeserializePartially to skip actions array when only reading level name | `optimizer/json` |

### optimizer/particle — Condition: `optimizeParticle`
Particle decoration optimization (culling, LOD, pooling).

| Class | Target | Description | Correct Path |
|---|---|---|---|
| `ParticleOptimizationPatches.ParticleCullingModePatch` | `scrParticleDecoration.ResetParticle` | Sets cullingMode = Pause for offscreen auto-pause | `optimizer/particle` |
| `ParticleOptimizationPatches.ParticleUpdateSkipPatch` | `scrParticleDecoration.Update` | Skips update for stopped/invisible particles; dirty-flag for scale/speed | `optimizer/particle` |
| `ParticleOptimizationPatches.ParticlePoolOnClearPatch` | `scrDecorationManager.ClearDecorations` | Pools particle GameObjects instead of destroying them | `optimizer/particle` |
| `ParticleOptimizationPatches.ParticlePoolOnCreatePatch` | `scrDecorationManager.CreateDecoration` | Reuses pooled particles when creating new ones | `optimizer/particle` |

### optimizer/multithread — Condition: `enableOptimizer`
Multithreaded event processing.

| Class | Target | Description | Correct Path |
|---|---|---|---|
| `MultithreadOptimizationPatches.ParallelEventProcessingPatch` | `scnGame.ApplyEventsToFloors` | Parallel event classification by floor | `optimizer/multithread` |

---

## BUGFIX PATCHES

### bugfix — Various conditions (compatibility settings)

| Class | Target | Description | Condition | Correct Path |
|---|---|---|---|---|
| `BugfixPatches.PortalTravelFixPatch` | `scrController.PortalTravelAction` | Prevents double-transition on portal travel | `portalTravelFix` | `bugfix/portal` |
| `BugfixPatches.AsyncInputPlaySnapPatch` | `scnGame.Play` | Snaps offsetTick calibration at level start | Always-on | `bugfix` |
| `BugfixPatches.EditorPlayResetMistakesPatch` | `scnGame.Play` | Resets hardestDifficulty when playing from editor | `fixEditorPlayResetMistakes` | `bugfix/editorPlayReset` |
| `BugfixPatches.TurnaroundConditionFix` | `scrLevelMaker.CalculateSingleFloorAngleLength` | Fixes turnaround detection to match v2.9.8 behavior | `fixTurnaroundCondition` | `bugfix/turnaround` |
| `BugfixPatches.CoopPauseHandleLockFix` | `scrPlanet.HandlePause` | Coop pause beat LockInput shouldn't block other players | `fixCoopPauseLock` | `bugfix/coopPause` |
| `BugfixPatches.CoopPauseLockFixPlayerPatch` | `scrPlayer.LockInput` | Skips LockInput in coop mode | `fixCoopPauseLock` | `bugfix/coopPause` |
| `BugfixPatches.CoopPauseLockFixControllerPatch` | `scrController.LockInput` | Skips LockInput in coop mode | `fixCoopPauseLock` | `bugfix/coopPause` |
| `BugfixPatches.CoopPlayerHitFix` | `scrPlayer.Hit` | Prevents hit during coop pause lock | `fixCoopPauseLock` | `bugfix/coopPause` |

---

## UI / MISC PATCHES

| Class | Target | Description | Condition | Correct Path |
|---|---|---|---|---|
| `MiscPatches.RemoveNewsPatch` | `scnLevelSelect` (Awake, Update) | Hides news container | `removeNews` | `ui/news` |
| `MiscPatches.HideBetaWatermarkPatch` | `scrEnableIfBeta.Awake` | Hides beta watermark | `hideBetaWatermark` | `ui/watermark` |
| `MiscPatches.ForceDifficultyUIPatch` | `scrMisc.DetermineDifficultyUIMode` | Shows all difficulty UI in CLS | `forceDifficultyUI` | `ui/difficulty` |
| `MiscPatches.CircleArcPatch` | `FloorMesh.SmallestAngleBetweenTwoAngles` | Enables circular arc rendering for 90-105 deg tiles | `enableCircleArc` | `ui/circleArc` |
| `MiscPatches.AutoplayTextPositionPatch` | `scrUIController.Update` | Moves autoplay text to custom position | `moveAutoplayText` | `ui/autoplayText` |
| `MiscPatches.AlwaysCountdownPatch` | `scnGame.Play` | Forces countdown even in auto-play mode | `alwaysCountdown` | `ui/countdown` |
| `MiscPatches.AutoplayHintUIPatch` | `scnEditor.Update` | Customizable autoplay hint text in editor | Always-on | `ui/autoplayHint` |
| `MiscPatches.LobbyMusicPatch` | `scnLevelSelect.Awake` | Custom lobby music replacement | `enableLobbyMusicPatch` | `ui/lobbyMusic` |
| `MiscPatches.CustomBpmPatch` | `scrConductor.Update` | Custom BPM for lobby music | `enableCustomBpm` | `ui/lobbyMusic` |
| `PausePlanetTrailPatch` | `PausePlanets.UpdateParticles` | Keeps planet particles alive during pause menu | `enablePausePlanetTrail` | `ui/pauseTrail` |

---

## COMPATIBILITY PATCHES

| Class | Target | Description | Condition | Correct Path |
|---|---|---|---|---|
| `CompatibilityPatches.LegacyPauseFixPatch_Play` | `scnEditor.Play` | Legacy pause fix for editor play | `enableLegacyPauseFix` | `compatibility/legacyPause` |
| `CompatibilityPatches.NoFailTooEarlyPatch` | `scrDecoration.HitboxTriggerAction` | Prevents no-fail too early hits | `enableNoFailTooEarly` | `compatibility/noFail` |
| `CompatibilityPatches.ScaleFilterSpeedWithPitchPatch` | All `CameraFilterPack*.OnRenderImage` | Scales filter shader animation speed with pitch | `scaleFilterSpeedWithPitch` | `compatibility/scaleFilter` |
| `CameraRelativeDragPatches` | `scnEditor.DragDecorations` | Fixes camera-relative decoration drag offset | `fixCameraRelativeDrag` | `compatibility/cameraDrag` |
| `JsonPatches.ForceAngleDataPatch` | `LevelData.Decode` | Converts pathData to angleData for compat | `forceAngleData` | `compatibility/forceAngle` |
| `JsonPatches.LegacyBehaviorPatch` | `LevelData.Decode` | Forces legacy flash/camRelativeTo behavior | `legacyFlashMode != Default \|\| legacyCamRelativeToMode != Default` | `compatibility/legacyBehavior` |

---

## REQUIRED MODS / CUSTOM EVENTS

| Class | Target | Description | Condition | Correct Path |
|---|---|---|---|---|
| `RequiredModsClearPatches.LevelDataClearPatch` | `LevelData.Decode` | Empties requiredMods before decode | `ignoreRequiredMods` | `compatibility/requiredMods` |
| `RequiredModsClearPatches.LevelDataCLSClearPatch` | `LevelDataCLS.Decode` | Empties requiredMods in CLS decode | `ignoreRequiredMods` | `compatibility/requiredMods` |
| `RequiredModsClearPatches.EncodeRestorePatch` | `LevelData.EncodeToDictionary` | Restores requiredMods before save | `ignoreRequiredMods` | `compatibility/requiredMods` |
| `RequiredModsClearPatches.LevelLoadNotifyPatch` | `scnGame.LoadLevel` | Notifies player of ignored missing mods | `ignoreRequiredMods` | `compatibility/requiredMods` |
| `CustomEventsPatches.ScanRegisterPatch` | `LevelData.Decode` | Scans and registers fake event infos for unknown event types | `ignoreRequiredMods` | `compatibility/customEvents` |
| `CustomEventsPatches.ScanRegisterCLSPatch` | `LevelDataCLS.Decode` | Same for CLS decode path | `ignoreRequiredMods` | `compatibility/customEvents` |
| `CustomEventsPatches.FakeEventDecodePatch` | `LevelEvent.Decode` | Decodes fake events with proper info/data | `ignoreRequiredMods` | `compatibility/customEvents` |
| `CustomEventsPatches.FakeEventEncodePatch` | `LevelEvent.Encode` | Encodes fake events with original eventType name | `ignoreRequiredMods` | `compatibility/customEvents` |
| `CustomEventsPatches.ReadOnlyPanelPatch` | `PropertiesPanel.Init` | Disables all controls for fake event panels | `ignoreRequiredMods` | `compatibility/customEvents` |
| `CustomEventsPatches.ListItemEventPatch` | `ListItem_Event.SetEvent` | Shows original event name and generic icon | `ignoreRequiredMods` | `compatibility/customEvents` |
| `CustomEventsPatches.EventIndicatorPatch` | `EventIndicator.Init` | Uses generic icon for fake event indicators | `ignoreRequiredMods` | `compatibility/customEvents` |
| `CustomEventsPatches.ShowPanelFakeEventPatch` | `InspectorPanel.ShowPanel` | Routes fake event selection to custom panel | `ignoreRequiredMods` | `compatibility/customEvents` |
| `CustomEventsPatches.RemoveEventAtSelectedPatch` | `scnEditor.RemoveEventAtSelected` | Handles deletion of fake events | `ignoreRequiredMods` | `compatibility/customEvents` |
| `CustomEventsPatches.ShowTabsForFloorPatch` | `InspectorPanel.ShowTabsForFloor` | Shows fake event tabs in inspector | `ignoreRequiredMods` | `compatibility/customEvents` |
| `CustomEventsPatches.FakeTabSetSelectedPatch` | `InspectorTab.SetSelected` | Hides cycleButtons for fake event tabs | `ignoreRequiredMods` | `compatibility/customEvents` |
| `CustomEventsPatches.FakeTabClickPatch` | `InspectorTab.OnPointerClick` | Routes fake tab clicks to ShowPanel(None, eventIndex) | `ignoreRequiredMods` | `compatibility/customEvents` |

---

## SOUND PATCHES

| Class | Target | Description | Condition | Correct Path |
|---|---|---|---|---|
| `HitSoundPatch` | `scnGame.Update` | Scales hit sound pitch with song pitch | `enableHitSoundPitch` | `sound/hitSound` |
| `JudgeTextPatches.HitTextMeshInitPatch` | `scrHitTextMesh.Init` | Custom judge text templates | `enableJudgeTextCustomization` | `sound/judgeText` |
| `JudgeTextPatches.HitTextMeshShowPatch` | `scrHitTextMesh.Show` | Replaces {offset} placeholders with timing | `enableJudgeTextCustomization` | `sound/judgeText` |
| `JudgeTextPatches.HitTextManagerShowPatch` | `scrHitTextManager.ShowHitText` | Captures missAngle for Show (non-coop) | Always-on | `sound/judgeTextRotation` |
| `JudgeTextPatches.HitTextMeshShowRotationFixPatch` | `scrHitTextMesh.Show` | Fixes non-coop missAngle rotation | `fixJudgeRotation` | `sound/judgeTextRotation` |

---

## EDITOR PATCHES

| Class | Target | Description | Condition | Correct Path |
|---|---|---|---|---|
| `EditorShortcutPatches.EditorShortcutUpdatePatch` | `scnEditor.Update` | Editor keyboard shortcuts (select all, deselect, toggle visibility, etc.) | `enableEditorShortcuts` | `editor/shortcuts` |
| `EditorShortcutPatches.FloorSelectCameraJumpPatch` | `scnEditor.SelectFloor` | Optional camera follow on floor select | `enableEditorShortcuts` | `editor/shortcuts` |
| `EditorPausePatches` | `scnEditor.Update` | Customizable pause hotkey in editor auto-play | Always-on | `editor/pause` |

---

## NON-PATCH UTILITY CLASSES (Not registered via PatchManager)

These classes exist in the patch files but are NOT registered as Harmony patches:

| Class | File | Purpose |
|---|---|---|
| `LoadingOptimizationPatches.TweenPoolManager` | LoadingOptimizationPatches.cs | Object pool for DOTween Tweens |
| `SceneOptimizationPatches.EditorUpdateScrollRectCachePatch` | SceneOptimizationPatches.cs | Utility for caching ScrollRect lookups (no HarmonyPatch attribute) |
| `TrackOptimizationPatches.CleanupPatch` | TrackOptimizationPatches.cs | Cleans up `_floorTransformCache` on Awake (no IriPatch attribute) |
| `MultithreadOptimizationPatches.DecorationCalculationCache` | MultithreadOptimizationPatches.cs | Parallel decoration transform pre-calculation cache |
| `DOTweenOptimizationPatches` | DOTweenOptimizationPatches.cs | Static helper for DOTween runtime settings (capacity, recyclable, safe mode) — no Harmony patches |

---

## Summary: Old PatchManager Registration → Correct Sub-Paths

The OLD PatchManager registered ALL nested types in these parent classes under `enableOptimizer`:

| Old Parent Type | Old Condition | Patches That Belong in... | Count |
|---|---|---|---|
| `OptimizerPatches` | `enableOptimizer` | `optimizer/texture` (2), `optimizer/decor` (10), `optimizer/scene` (3), `optimizer` (3) | 18 |
| `TrackOptimizationPatches` | `enableOptimizer` | `optimizer/track` (2) | 2 |
| `FfxOptimizationPatches` | `enableOptimizer` | `optimizer/ffx` (1) | 1 |
| `SceneOptimizationPatches` | `enableOptimizer` | `optimizer/scene` (5) | 5 |
| `LoadingOptimizationPatches` | `enableOptimizer` (+ sub-condition for frame-spread) | `optimizer/loading` (6) | 6 |
| `ExtremeOptimizationPatches` | `enableOptimizer && enableExtremeOptimization` | `optimizer/extreme` (2) | 2 |
| `TweenSafetyPatches` | `enableOptimizer && dotweenDefaultRecyclable` | `optimizer/tweenSafety` (4) | 4 |
| `CustomEasingPatches` | `enableCustomEasingEngine` | `optimizer/customEasing` (4) | 4 |

Individually registered (old system):
| Old Registration | Old Condition | Correct Path |
|---|---|---|
| `EventTweenOptimizationPatches.*` (4 patches) | `optimizeEventProcessing` | `optimizer/eventTween` |
| `JsonPatches.PatchGetCustomLevelName` | `customLevelReadOptimization` | `optimizer/json` |
| `PlayerInputOptimizationPatches.SimulatedPlayerControlUpdatePatch` | `optimizePlayerInputAllocations` | `optimizer/playerInput` |
| `RDInputOptimizationPatches.GetStateKeysPatch` | `optimizeRDInputAllocations` | `optimizer/rdInput` |
| `EditorFloorOptimizationPatches.*` (10 patches) | `enableEditorFloorOptimization` + sub-conditions | `optimizer/editorFloor/insert` |
| `AdaptivePatches.TextureNameCleanup` | `enableOptimizer` | `optimizer/texture` |
| `AdaptivePatches.DecorationScalingCustomSprite` | `enableOptimizer` | `optimizer/decor` |
| `AdaptivePatches.DecorationScalingSprite` | `enableOptimizer` | `optimizer/decor` |
