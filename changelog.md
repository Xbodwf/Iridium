# Changelog

## [Unreleased]

### Added
- **Custom level JSON read optimization**: Transpiler replaces `Json.Deserialize` → `Json.DeserializePartially(str, "actions")` in `LevelDataCLS.LoadLevel` (main + frontline) and `LevelData.GetCustomLevelName` (frontline only). Skips parsing the entire `actions` array when only settings metadata is needed. Includes setting toggle `customLevelReadOptimization` with i18n support.
- **Bugfix patches (frontline v2.10.0)**: `PortalTravelFix` for portal travel crashes, `SyncSpeedTrialOnLoad` for speed trial background sync.
- **Editor pause shortcut (frontline v2.10.0)**: Custom pause key with modifier support (Ctrl/Shift/Alt/Win) in Compatibility tab.

### Changed
- **Removed DebugLog instrumentation** from `LoadingOptimizationPatches`.
- **Unified Iridium_frontline.csproj**: deleted duplicate `Iridium.csproj` in frontline.
