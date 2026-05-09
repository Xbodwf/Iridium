using System;
using UnityModManagerNet;
using UnityEngine;
using Iridium.UI;
using Iridium.Config;
using Iridium.Patches;
using System.IO;
using System.Linq;

namespace Iridium
{
    public class Settings : UnityModManager.ModSettings
    {
        public string language = "en";
        public bool firstRun = true;
        public string? lastVersion = null; // 用于跟踪版本升级
        public string? lastUpgradeMessageSeen_106_beta5 = null; // 记录用户最后看过的特定升级提示 ID
        
        public OptimizerSettings optimizer = new();
        public UISettings ui = new();
        public LobbyMusicSettings lobbyMusic = new();
        public MemorySettings memory = new();
        public CompatibilitySettings compatibility = new();
        public HitSoundSettings hitSound = new();
        public JudgeTextSettings judgeText = new();
        public PatchModeSettings patchMode = new();

        private string? _defaultLobbyMusicPathCache;
        private string? _fastLobbyMusicPathCache;

        // 性能优化：跟踪帧数
        private static int _lastFrameCount = -1;

        // 标签页状态
        private int _currentTabIndex = 0;
        private Vector2 _contentScrollPosition = Vector2.zero;

        // 样式缓存
        private GUIStyle? _sidebarStyle;
        private GUIStyle? _sidebarHeaderStyle;
        private GUIStyle? _sidebarLanguageStyle;
        private GUIStyle? _versionStyle;

        private GUIStyle SidebarStyle
        {
            get
            {
                if (_sidebarStyle == null)
                {
                    _sidebarStyle = new(GUI.skin.box)
                    {
                        normal = { background = UIUtils.GetCachedRoundedTex(32, 32, 0, UILayout.SidebarBgColor) },
                        padding = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(0, 0, 0, 0)
                    };
                }
                return _sidebarStyle;
            }
        }

        private GUIStyle SidebarHeaderStyle
        {
            get
            {
                if (_sidebarHeaderStyle == null)
                {
                    _sidebarHeaderStyle = new(GUI.skin.box)
                    {
                        normal = { background = UIUtils.GetCachedRoundedTex(32, 32, 16, new Color(0.16f, 0.10f, 0.16f)) },
                        padding = new RectOffset(16, 16, 16, 16),
                        margin = new RectOffset(8, 8, 8, 8)
                    };
                }
                return _sidebarHeaderStyle;
            }
        }

        private GUIStyle SidebarLanguageStyle
        {
            get
            {
                if (_sidebarLanguageStyle == null)
                {
                    _sidebarLanguageStyle = new(GUI.skin.box)
                    {
                        normal = { background = UIUtils.GetCachedRoundedTex(32, 32, 16, new Color(0.16f, 0.10f, 0.16f)) },
                        padding = new RectOffset(12, 12, 12, 12),
                        margin = new RectOffset(8, 8, 8, 8)
                    };
                }
                return _sidebarLanguageStyle;
            }
        }

        private GUIStyle VersionStyle
        {
            get
            {
                if (_versionStyle == null)
                {
                    _versionStyle = new(UIUtils.LabelStyle)
                    {
                        alignment = TextAnchor.MiddleRight,
                        normal = { textColor = new Color(0.5f, 0.5f, 0.5f, 0.5f) }
                    };
                }
                return _versionStyle;
            }
        }

        public void OnGUI(UnityModManager.ModEntry modEntry)
        {
            // 性能优化：只在必要时初始化样式
            UIUtils.InitializeStyles();

            // 性能优化：只在第一帧或需要时执行某些检查
            if (Time.frameCount != _lastFrameCount)
            {
                _lastFrameCount = Time.frameCount;
            }

            _defaultLobbyMusicPathCache ??= lobbyMusic.defaultMusicPath;
            _fastLobbyMusicPathCache ??= lobbyMusic.fastMusicPath;

            GUILayout.BeginHorizontal();

            // --- Left Sidebar Navigation ---
            GUILayout.BeginVertical(SidebarStyle, GUILayout.Width(UILayout.SIDEBAR_WIDTH));

            // Sidebar Header
            GUILayout.BeginVertical(SidebarHeaderStyle, GUILayout.Height(UILayout.SIDEBAR_HEADER_HEIGHT));
            GUILayout.Label("Iridium", UIUtils.HeaderStyle);
            GUILayout.Label("Settings", UIUtils.LabelSecondaryStyle);
            GUILayout.EndVertical();

            GUILayout.Space(8);

            // Navigation Items
            string[] tabNames = [
                "🚀 " + Localization.Get("EnableOptimizer"),
                "🎨 " + Localization.Get("UISettings"),
                "🎵 " + Localization.Get("LevelSelectSettings"),
                "🔧 " + Localization.Get("CompatibilitySettings"),
                "⚙️ " + Localization.Get("HitSoundSettings"),
                "🧩 Patch System"
            ];

            for (int i = 0; i < tabNames.Length; i++)
            {
                if (UILayout.DrawSidebarItem(tabNames[i], _currentTabIndex == i))
                {
                    _currentTabIndex = i;
                    _contentScrollPosition = Vector2.zero;
                }
            }

            GUILayout.FlexibleSpace();

            // Processing Status Indicator (if active)
            if (Iridium.Patches.AsyncPatchManager.IsProcessing)
            {
                GUILayout.BeginVertical(SidebarLanguageStyle);
                GUILayout.Label("⏳ " + Localization.Get("AsyncPatchProcessing"), UIUtils.LabelSecondaryStyle);
                GUILayout.EndVertical();
                GUILayout.Space(8);
            }

            // Language Selection at Bottom
            GUILayout.BeginVertical(SidebarLanguageStyle);
            GUILayout.Label(Localization.Get("Language"), UIUtils.LabelStyle);
            GUILayout.BeginVertical();
            var langs = Localization.AvailableLanguages;
            foreach (var lang in langs)
            {
                bool isCurrent = language == lang;
                if (isCurrent) GUI.color = UIUtils.Primary;
                string displayName = Localization.GetDisplayName(lang);
                if (GUILayout.Button(displayName.ToUpper(), UIUtils.LanguageButtonStyle, GUILayout.Height(32), GUILayout.ExpandWidth(true)))
                {
                    language = lang;
                }
                GUI.color = Color.white;
                GUILayout.Space(2);
            }
            GUILayout.EndVertical();
            GUILayout.EndVertical();

            GUILayout.EndVertical(); // End Sidebar

            GUILayout.Space(16);

            // --- Right Content Area ---
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            // Content Header
            switch (_currentTabIndex)
            {
                case 0:
                    UILayout.DrawContentHeader(Localization.Get("EnableOptimizer"), Localization.Get("OptimizerDescription"));
                    break;
                case 1:
                    UILayout.DrawContentHeader(Localization.Get("UISettings"), Localization.Get("UISettingsDescription"));
                    break;
                case 2:
                    UILayout.DrawContentHeader(Localization.Get("LevelSelectSettings"), Localization.Get("LevelSelectDescription"));
                    break;
                case 3:
                    UILayout.DrawContentHeader(Localization.Get("CompatibilitySettings"), Localization.Get("CompatibilityDescription"));
                    break;
                case 4:
                    UILayout.DrawContentHeader(Localization.Get("HitSoundSettings"), Localization.Get("OtherOptionsDescription"));
                    break;
                case 5:
                    UILayout.DrawContentHeader("Patch System", "Runtime IL mode switching for hot-path patches");
                    break;
            }

            // Content Scroll Area
            _contentScrollPosition = GUILayout.BeginScrollView(_contentScrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            // Tab Content
            switch (_currentTabIndex)
            {
                case 0:
                    DrawOptimizerTab();
                    break;
                case 1:
                    DrawUISettingsTab();
                    break;
                case 2:
                    DrawLevelSelectTab();
                    break;
                case 3:
                    DrawCompatibilityTab();
                    break;
                case 4:
                    DrawHitSoundAndJudgeTextTab();
                    break;
                case 5:
                    DrawPatchSystemTab();
                    break;
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical(); // End Content Area

            GUILayout.EndHorizontal();

            GUILayout.Space(16);

            // Version Info
            GUILayout.Label($"Iridium {VersionManager.GetFullVersionString()}", VersionStyle);

            if (GUI.changed)
            {
                Save(modEntry);
            }
        }

        private void DrawOptimizerTab()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("EnableOptimizer"), UIUtils.LabelStyle);
            GUILayout.FlexibleSpace();
            bool newEnableOptimizer = UIUtils.M3Switch(optimizer.enableOptimizer, "");
            if (newEnableOptimizer != optimizer.enableOptimizer)
            {
                optimizer.enableOptimizer = newEnableOptimizer;
                if (optimizer.enableOptimizer && optimizer.disableShadows) QualitySettings.shadows = ShadowQuality.Disable;
                else QualitySettings.shadows = ShadowQuality.All;
                Iridium.Patches.AsyncPatchManager.UpdateOptimizerPatchesAsync();
            }
            GUILayout.EndHorizontal();

            if (optimizer.enableOptimizer)
            {
                GUILayout.Space(8);

                if (Iridium.Patches.OptimizerPatches.savedVRAM_MB > 0.1f)
                {
                    UIUtils.DrawInfoBox("✨ " + Localization.Get("SavedMemoryMsg", Iridium.Patches.OptimizerPatches.savedVRAM_MB.ToString("F2")));
                    GUILayout.Space(8);
                }

                UILayout.DrawSettingGroupTitle(Localization.Get("ImageOptimizations"));

                bool showSavedMemory = UIUtils.M3Switch(!optimizer.dontShowSavedMemory, Localization.Get("ShowSavedMemory"));
                if (showSavedMemory == optimizer.dontShowSavedMemory)
                {
                    optimizer.dontShowSavedMemory = !showSavedMemory;
                }

                bool compressImage = UIUtils.M3Switch(!optimizer.dontCompress, Localization.Get("CompressImage"));
                if (compressImage == optimizer.dontCompress)
                {
                    optimizer.dontCompress = !compressImage;
                }

                bool multipleOf4 = UIUtils.M3Switch(!optimizer.dontResizeMultipleOf4, Localization.Get("MultipleOf4"));
                if (multipleOf4 == optimizer.dontResizeMultipleOf4)
                {
                    optimizer.dontResizeMultipleOf4 = !multipleOf4;
                }

                if (optimizer.dontCompress) optimizer.dontResizeMultipleOf4 = true;

                GUILayout.Space(8);
                GUILayout.BeginHorizontal(GUILayout.Height(28));
                GUILayout.Label(Localization.Get("DivideImageBy"), UIUtils.LabelStyle);
                GUILayout.FlexibleSpace();
                string divideByStr = GUILayout.TextField(optimizer.divideBy.ToString("F1"), 5, UIUtils.TextFieldStyle, GUILayout.Width(50));
                if (double.TryParse(divideByStr, out double newDivideBy)) optimizer.divideBy = newDivideBy;
                GUILayout.EndHorizontal();

                GUILayout.Space(4);
                optimizer.dontResizeCollider = UIUtils.M3Switch(optimizer.dontResizeCollider, Localization.Get("DontResizeCollider"));

                UILayout.DrawSettingGroupTitle(Localization.Get("RenderingOptimizations"));

                bool newDisableShadows = UIUtils.M3Switch(optimizer.disableShadows, Localization.Get("DisableShadows"));
                if (newDisableShadows != optimizer.disableShadows)
                {
                    optimizer.disableShadows = newDisableShadows;
                    if (optimizer.enableOptimizer && optimizer.disableShadows) QualitySettings.shadows = ShadowQuality.Disable;
                    else QualitySettings.shadows = ShadowQuality.All;
                }

                optimizer.optimizeDecorationUpdate = UIUtils.M3Switch(optimizer.optimizeDecorationUpdate, Localization.Get("OptimizeDecorationUpdate"));
                optimizer.optimizeTileUpdate = UIUtils.M3Switch(optimizer.optimizeTileUpdate, Localization.Get("OptimizeTileUpdate"));
                optimizer.optimizeMoveTrack = UIUtils.M3Switch(optimizer.optimizeMoveTrack, Localization.Get("OptimizeMoveTrack"));
                optimizer.optimizeRecolorTrack = UIUtils.M3Switch(optimizer.optimizeRecolorTrack, Localization.Get("OptimizeRecolorTrack"));
                optimizer.skipEventIfPaused = UIUtils.M3Switch(optimizer.skipEventIfPaused, Localization.Get("SkipEventIfPaused"));
                optimizer.optimizeEventIcons = UIUtils.M3Switch(optimizer.optimizeEventIcons, Localization.Get("OptimizeEventIcons"));
                optimizer.optimizeScnGameUpdate = UIUtils.M3Switch(optimizer.optimizeScnGameUpdate, Localization.Get("OptimizeScnGameUpdate"));
                optimizer.optimizeMoveDecorations = UIUtils.M3Switch(optimizer.optimizeMoveDecorations, Localization.Get("OptimizeMoveDecorations"));
                optimizer.optimizeFfxDecorations = UIUtils.M3Switch(optimizer.optimizeFfxDecorations, Localization.Get("OptimizeFfxDecorations"));
                optimizer.optimizeFloorMesh = UIUtils.M3Switch(optimizer.optimizeFloorMesh, Localization.Get("OptimizeFloorMesh"));
                optimizer.optimizeFilters = UIUtils.M3Switch(optimizer.optimizeFilters, Localization.Get("OptimizeFilters"));
                optimizer.fastLoading = UIUtils.M3Switch(optimizer.fastLoading, Localization.Get("FastLoading"));

                UILayout.DrawSettingGroupTitle(Localization.Get("SceneOptimizations"));

                optimizer.cacheGameObjectReferences = UIUtils.M3Switch(optimizer.cacheGameObjectReferences, Localization.Get("CacheGameObjectReferences"));
                optimizer.optimizeEventProcessing = UIUtils.M3Switch(optimizer.optimizeEventProcessing, Localization.Get("OptimizeEventProcessing"));
                optimizer.optimizeEditorMouseDetection = UIUtils.M3Switch(optimizer.optimizeEditorMouseDetection, Localization.Get("OptimizeEditorMouseDetection"));
                optimizer.optimizeEditorEventIndicators = UIUtils.M3Switch(optimizer.optimizeEditorEventIndicators, Localization.Get("OptimizeEditorEventIndicators"));

                UILayout.DrawSettingGroupTitle(Localization.Get("LoadingOptimizations"));

                optimizer.cacheFloorEvents = UIUtils.M3Switch(optimizer.cacheFloorEvents, Localization.Get("CacheFloorEvents"));
                optimizer.optimizeMoveTrackTweens = UIUtils.M3Switch(optimizer.optimizeMoveTrackTweens, Localization.Get("OptimizeMoveTrackTweens"));
                optimizer.batchMoveDecorations = UIUtils.M3Switch(optimizer.batchMoveDecorations, Localization.Get("BatchMoveDecorations"));

                UILayout.DrawSettingGroupTitle(Localization.Get("DOTweenOptimizations"));

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localization.Get("EnableDOTweenOptimization"), UIUtils.LabelStyle);
                GUILayout.FlexibleSpace();
                bool newOptimizeDOTween = UIUtils.M3Switch(optimizer.optimizeDOTweenGlobal, "");
                if (newOptimizeDOTween != optimizer.optimizeDOTweenGlobal)
                {
                    optimizer.optimizeDOTweenGlobal = newOptimizeDOTween;
                    
                    if (newOptimizeDOTween)
                    {
                        // 启用：应用优化设置
                        Iridium.Patches.DOTweenOptimizationPatches.ApplyRuntimeSettings();
                    }
                    else
                    {
                        // 禁用：恢复默认设置
                        Iridium.Patches.DOTweenOptimizationPatches.ResetRuntimeSettings();
                    }
                    
                    // 注意：DOTween优化现在不使用补丁，不需要更新补丁列表
                }
                GUILayout.EndHorizontal();

                if (optimizer.optimizeDOTweenGlobal)
                {
                    GUILayout.Space(8);

                    GUILayout.BeginHorizontal(GUILayout.Height(28));
                    GUILayout.Label(Localization.Get("TweenerCapacity"), UIUtils.LabelStyle);
                    GUILayout.FlexibleSpace();
                    string tweenerCapStr = GUILayout.TextField(optimizer.dotweenTweenerCapacity.ToString(), 5, UIUtils.TextFieldStyle, GUILayout.Width(60));
                    if (int.TryParse(tweenerCapStr, out int newTweenerCap))
                    {
                        optimizer.dotweenTweenerCapacity = Mathf.Clamp(newTweenerCap, 200, 2000);
                        Iridium.Patches.DOTweenOptimizationPatches.ApplyRuntimeSettings();
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(GUILayout.Height(28));
                    GUILayout.Label(Localization.Get("SequenceCapacity"), UIUtils.LabelStyle);
                    GUILayout.FlexibleSpace();
                    string seqCapStr = GUILayout.TextField(optimizer.dotweenSequenceCapacity.ToString(), 5, UIUtils.TextFieldStyle, GUILayout.Width(60));
                    if (int.TryParse(seqCapStr, out int newSeqCap))
                    {
                        optimizer.dotweenSequenceCapacity = Mathf.Clamp(newSeqCap, 50, 500);
                        Iridium.Patches.DOTweenOptimizationPatches.ApplyRuntimeSettings();
                    }
                    GUILayout.EndHorizontal();

                    bool newRecyclable = UIUtils.M3Switch(optimizer.dotweenDefaultRecyclable, Localization.Get("DOTweenDefaultRecyclable"));
                    if (newRecyclable != optimizer.dotweenDefaultRecyclable)
                    {
                        optimizer.dotweenDefaultRecyclable = newRecyclable;
                        Iridium.Patches.DOTweenOptimizationPatches.ApplyRuntimeSettings();
                    }

                    bool newDisableSafeMode = UIUtils.M3Switch(optimizer.dotweenDisableSafeMode, Localization.Get("DOTweenDisableSafeMode"));
                    if (newDisableSafeMode != optimizer.dotweenDisableSafeMode)
                    {
                        optimizer.dotweenDisableSafeMode = newDisableSafeMode;
                        Iridium.Patches.DOTweenOptimizationPatches.ApplyRuntimeSettings();
                    }
                }

                // 提示信息
                GUILayout.Space(4);
                GUI.contentColor = Color.yellow;
                GUILayout.Label(Localization.Get("DOTweenOptimizationRestartRequired"), UIUtils.LabelStyle);
                GUI.contentColor = Color.white;

                UILayout.DrawSettingGroupTitle(Localization.Get("ExtremeOptimizations"));

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localization.Get("EnableExtremeOptimization"), UIUtils.LabelStyle);
                GUILayout.FlexibleSpace();
                bool newEnableExtreme = UIUtils.M3Switch(optimizer.enableExtremeOptimization, "");
                if (newEnableExtreme != optimizer.enableExtremeOptimization)
                {
                    optimizer.enableExtremeOptimization = newEnableExtreme;
                    Iridium.Patches.AsyncPatchManager.UpdateOptimizerPatchesAsync();
                }
                GUILayout.EndHorizontal();

                if (optimizer.enableExtremeOptimization)
                {
                    GUILayout.Space(8);

                    GUILayout.BeginHorizontal(GUILayout.Height(28));
                    GUILayout.Label(Localization.Get("MaxTweensPerFrame"), UIUtils.LabelStyle);
                    GUILayout.FlexibleSpace();
                    string maxTweensStr = GUILayout.TextField(optimizer.maxTweensPerFrame.ToString(), 5, UIUtils.TextFieldStyle, GUILayout.Width(60));
                    if (int.TryParse(maxTweensStr, out int newMaxTweens))
                    {
                        optimizer.maxTweensPerFrame = Mathf.Clamp(newMaxTweens, 50, 500);
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Space(4);
                    GUI.contentColor = Color.cyan;
                    GUILayout.Label("极端优化：分帧处理大量并发事件，避免单帧卡顿", UIUtils.LabelStyle);
                    GUI.contentColor = Color.white;
                }

                GUILayout.Space(8);

                // Error states
                if (typeof(Notification).GetMethod("SetupNotification", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) == null)
                {
                    GUILayout.Space(4);
                    UIUtils.DrawInfoBox("⚠ " + Localization.Get("MethodNotFound", "Notification.SetupNotification"), true);
                    optimizer.dontShowSavedMemory = true;
                }
                if (typeof(scrVisualDecoration).GetProperty("spriteUnscaledSize", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public) == null)
                {
                    GUILayout.Space(4);
                    UIUtils.DrawInfoBox("⚠ " + Localization.Get("PropertyNotFound", "scrVisualDecoration.spriteUnscaledSize"), true);
                    optimizer.dontResizeCollider = true;
                }
            }

            UILayout.DrawSettingGroupTitle(Localization.Get("MemorySettings"));

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("MemorySettings"), UIUtils.LabelStyle);
            GUILayout.FlexibleSpace();
            bool newEnableMemory = UIUtils.M3Switch(memory.enableMemoryOptimization, "");
            if (newEnableMemory != memory.enableMemoryOptimization)
            {
                memory.enableMemoryOptimization = newEnableMemory;
                Iridium.Patches.AsyncPatchManager.UpdatePatchByTypeAsync(typeof(Iridium.Patches.MiscPatches.SmartGCPatch));
            }
            GUILayout.EndHorizontal();

            if (memory.enableMemoryOptimization)
            {
                GUILayout.Space(8);

                bool newEnableSmartGC = UIUtils.M3Switch(memory.enableSmartGC, Localization.Get("EnableSmartGC"));
                if (newEnableSmartGC != memory.enableSmartGC)
                {
                    memory.enableSmartGC = newEnableSmartGC;
                    Iridium.Patches.AsyncPatchManager.UpdatePatchByTypeAsync(typeof(Iridium.Patches.MiscPatches.SmartGCPatch));
                }
                if (memory.enableSmartGC)
                {
                    GUILayout.BeginHorizontal(GUILayout.Height(28));
                    GUILayout.Space(24);
                    GUILayout.Label(Localization.Get("GCInterval"), UIUtils.LabelStyle);
                    GUILayout.FlexibleSpace();
                    string intervalStr = GUILayout.TextField(memory.gcInterval.ToString("F0"), 4, UIUtils.TextFieldStyle, GUILayout.Width(50));
                    if (float.TryParse(intervalStr, out float newInterval)) memory.gcInterval = Mathf.Clamp(newInterval, 10f, 3600f);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(24);
                    memory.gcInGame = UIUtils.M3Switch(memory.gcInGame, Localization.Get("GCInGame"));
                    GUILayout.EndHorizontal();
                }
            }
        }

        private void DrawUISettingsTab()
        {
            bool newRemoveNews = UIUtils.M3Switch(ui.removeNews, Localization.Get("RemoveNews"));
            if (newRemoveNews != ui.removeNews)
            {
                ui.removeNews = newRemoveNews;
                Iridium.Patches.AsyncPatchManager.UpdatePatchByTypeAsync(typeof(Iridium.Patches.MiscPatches.RemoveNewsPatch));
                Iridium.Patches.MiscPatches.RemoveNewsPatch.UpdateNews();
            }
            bool newHideBeta = UIUtils.M3Switch(ui.hideBetaWatermark, Localization.Get("HideBetaWatermark"));
            if (newHideBeta != ui.hideBetaWatermark)
            {
                ui.hideBetaWatermark = newHideBeta;
                Iridium.Patches.AsyncPatchManager.UpdatePatchByTypeAsync(typeof(Iridium.Patches.StdHideBetaWatermarkPatch));
                Iridium.Patches.MiscPatches.RefreshBetaWatermark();
            }
            bool newForceDifficulty = UIUtils.M3Switch(ui.forceDifficultyUI, Localization.Get("ForceDifficultyUI"));
            if (newForceDifficulty != ui.forceDifficultyUI)
            {
                ui.forceDifficultyUI = newForceDifficulty;
                Iridium.Patches.AsyncPatchManager.UpdatePatchByTypeAsync(typeof(Iridium.Patches.StdForceDifficultyUIPatch));
            }
            ui.alwaysCountdown = UIUtils.M3Switch(ui.alwaysCountdown, Localization.Get("AlwaysCountdown"));

            bool newMoveAutoplay = UIUtils.M3Switch(ui.moveAutoplayText, Localization.Get("MoveAutoplayText"));
            if (newMoveAutoplay != ui.moveAutoplayText)
            {
                ui.moveAutoplayText = newMoveAutoplay;
                Iridium.Patches.AsyncPatchManager.UpdatePatchByTypeAsync(typeof(Iridium.Patches.StdAutoplayTextPositionPatch));
                Iridium.Patches.MiscPatches.RefreshAutoplayTextPosition();
            }

            if (ui.moveAutoplayText)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("X:", GUILayout.Width(20));
                ui.autoplayTextX = GUILayout.HorizontalSlider(ui.autoplayTextX, -Screen.width / 2f, Screen.width / 2f);
                GUILayout.Label(ui.autoplayTextX.ToString("F0"), UIUtils.LabelStyle, GUILayout.Width(40));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Y:", GUILayout.Width(20));
                ui.autoplayTextY = GUILayout.HorizontalSlider(ui.autoplayTextY, -Screen.height / 2f, Screen.height / 2f);
                GUILayout.Label(ui.autoplayTextY.ToString("F0"), UIUtils.LabelStyle, GUILayout.Width(40));
                GUILayout.EndHorizontal();

                if (GUI.changed)
                {
                    Iridium.Patches.MiscPatches.RefreshAutoplayTextPosition();
                }
            }

            GUILayout.Space(8);
            bool newEnableCircleArc = UIUtils.M3Switch(ui.enableCircleArc, Localization.Get("EnableCircleArc"));
            if (newEnableCircleArc != ui.enableCircleArc)
            {
                ui.enableCircleArc = newEnableCircleArc;
                Iridium.Patches.AsyncPatchManager.UpdatePatchByTypeAsync(typeof(Iridium.Patches.MiscPatches.CircleArcPatch));
            }
            if (ui.enableCircleArc) UIUtils.DrawInfoBox("⚠ " + Localization.Get("RestartRequired"), true);
        }

        private void DrawLevelSelectTab()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("LevelSelectSettings"), UIUtils.LabelStyle);
            GUILayout.FlexibleSpace();
            bool newEnableLobbyMusic = UIUtils.M3Switch(lobbyMusic.enableLobbyMusicPatch, "");
            if (newEnableLobbyMusic != lobbyMusic.enableLobbyMusicPatch)
            {
                lobbyMusic.enableLobbyMusicPatch = newEnableLobbyMusic;
                Iridium.Patches.AsyncPatchManager.UpdatePatchByTypeAsync(typeof(Iridium.Patches.MiscPatches.LobbyMusicPatch));
                if (lobbyMusic.enableLobbyMusicPatch)
                {
                    Iridium.Patches.MiscPatches.LobbyMusicPatch.ReloadFromSettings();
                }
            }
            GUILayout.EndHorizontal();

            if (lobbyMusic.enableLobbyMusicPatch)
            {
                GUILayout.Space(8);
                lobbyMusic.enableCustomBpm = UIUtils.M3Switch(lobbyMusic.enableCustomBpm, Localization.Get("EnableCustomBpm"));
                if (lobbyMusic.enableCustomBpm)
                {
                    GUILayout.BeginHorizontal(GUILayout.Height(28));
                    GUILayout.Label(Localization.Get("CustomBpm"), UIUtils.LabelStyle);
                    GUILayout.FlexibleSpace();
                    string bpmStr = GUILayout.TextField(lobbyMusic.customBpm.ToString("F1"), 6, UIUtils.TextFieldStyle, GUILayout.Width(60));
                    if (float.TryParse(bpmStr, out float newBpm)) lobbyMusic.customBpm = Mathf.Max(1f, newBpm);
                    GUILayout.EndHorizontal();
                }

                GUILayout.Space(4);
                lobbyMusic.fastMusic = UIUtils.M3Switch(lobbyMusic.fastMusic, Localization.Get("LobbyFastMusic"));

                bool newCustomMusic = UIUtils.M3Switch(lobbyMusic.customMusic, Localization.Get("LobbyCustomMusic"));
                if (newCustomMusic != lobbyMusic.customMusic)
                {
                    lobbyMusic.customMusic = newCustomMusic;
                    Iridium.Patches.MiscPatches.LobbyMusicPatch.ReloadFromSettings();
                }

                if (lobbyMusic.customMusic)
                {
                    GUILayout.BeginHorizontal(GUILayout.Height(28));
                    GUILayout.Label(Localization.Get("LobbyDefaultMusicPath"), UIUtils.LabelStyle);
                    GUILayout.FlexibleSpace();
                    _defaultLobbyMusicPathCache = GUILayout.TextField(_defaultLobbyMusicPathCache ?? string.Empty, UIUtils.TextFieldStyle, GUILayout.Width(260));
                    GUILayout.Space(6);
                    bool canApplyDefaultPath = (_defaultLobbyMusicPathCache ?? string.Empty) != lobbyMusic.defaultMusicPath;
                    GUI.enabled = canApplyDefaultPath;
                    if (GUILayout.Button(Localization.Get("Apply"), UIUtils.ButtonStyle, GUILayout.Width(70)))
                    {
                        lobbyMusic.defaultMusicPath = (_defaultLobbyMusicPathCache ?? string.Empty).Trim();
                        Iridium.Patches.MiscPatches.LobbyMusicPatch.StartLoad(true, lobbyMusic.defaultMusicPath);
                    }
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(GUILayout.Height(28));
                    GUILayout.Label(Localization.Get("LobbyFastMusicPath"), UIUtils.LabelStyle);
                    GUILayout.FlexibleSpace();
                    _fastLobbyMusicPathCache = GUILayout.TextField(_fastLobbyMusicPathCache ?? string.Empty, UIUtils.TextFieldStyle, GUILayout.Width(260));
                    GUILayout.Space(6);
                    bool canApplyFastPath = (_fastLobbyMusicPathCache ?? string.Empty) != lobbyMusic.fastMusicPath;
                    GUI.enabled = canApplyFastPath;
                    if (GUILayout.Button(Localization.Get("Apply"), UIUtils.ButtonStyle, GUILayout.Width(70)))
                    {
                        lobbyMusic.fastMusicPath = (_fastLobbyMusicPathCache ?? string.Empty).Trim();
                        Iridium.Patches.MiscPatches.LobbyMusicPatch.StartLoad(false, lobbyMusic.fastMusicPath);
                    }
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();

                    GUILayout.Space(6);
                    if (GUILayout.Button(Localization.Get("LobbyReloadMusic"), UIUtils.ButtonStyle, GUILayout.Width(140)))
                    {
                        Iridium.Patches.MiscPatches.LobbyMusicPatch.ReloadFromSettings();
                    }
                    UIUtils.DrawInfoBox(Localization.Get("LobbyMusicHint"));
                }
            }
        }

        private void DrawCompatibilityTab()
        {
            bool newLegacyPauseFix = UIUtils.M3Switch(compatibility.enableLegacyPauseFix, Localization.Get("EnableLegacyPauseFix"));
            if (newLegacyPauseFix != compatibility.enableLegacyPauseFix)
            {
                compatibility.enableLegacyPauseFix = newLegacyPauseFix;
                Iridium.Patches.AsyncPatchManager.UpdatePatchByTypeAsync(typeof(Iridium.Patches.CompatibilityPatches.LegacyPauseFixPatch_Play));
                Iridium.Patches.AsyncPatchManager.UpdatePatchByTypeAsync(typeof(Iridium.Patches.CompatibilityPatches.LegacyPauseFixPatch_Apply));
            }
            bool newNoFailTooEarly = UIUtils.M3Switch(compatibility.enableNoFailTooEarly, Localization.Get("EnableNoFailTooEarly"));
            if (newNoFailTooEarly != compatibility.enableNoFailTooEarly)
            {
                compatibility.enableNoFailTooEarly = newNoFailTooEarly;
                Iridium.Patches.AsyncPatchManager.UpdatePatchByTypeAsync(typeof(Iridium.Patches.CompatibilityPatches.NoFailTooEarlyPatch));
            }

            UILayout.DrawSettingGroupTitle(Localization.Get("LegacyLevelBehavior"));

            bool newForceAngleData = UIUtils.M3Switch(compatibility.forceAngleData, Localization.Get("ForceAngleData"));
            if (newForceAngleData != compatibility.forceAngleData)
            {
                compatibility.forceAngleData = newForceAngleData;
                Iridium.Patches.AsyncPatchManager.UpdatePatchByTypeAsync(typeof(Iridium.Patches.JsonPatches.ForceAngleDataPatch));
            }

            GUILayout.Space(8);

            GUILayout.Label(Localization.Get("LegacyFlashMode"), UIUtils.SubHeaderStyle);
            GUILayout.Space(2);
            var newFlashMode = (LegacyBehaviorMode)UIUtils.M3SegmentedButton((int)compatibility.legacyFlashMode,
                [Localization.Get("ModeDefault"), Localization.Get("ModeAlwaysOff"), Localization.Get("ModeAlwaysOn")]);

            GUILayout.Space(10);
            GUILayout.Label(Localization.Get("LegacyCamRelativeToMode"), UIUtils.SubHeaderStyle);
            GUILayout.Space(2);
            var newCamRelMode = (LegacyBehaviorMode)UIUtils.M3SegmentedButton((int)compatibility.legacyCamRelativeToMode,
                [Localization.Get("ModeDefault"), Localization.Get("ModeAlwaysOff"), Localization.Get("ModeAlwaysOn")]);

            if (newFlashMode != compatibility.legacyFlashMode || newCamRelMode != compatibility.legacyCamRelativeToMode)
            {
                compatibility.legacyFlashMode = newFlashMode;
                compatibility.legacyCamRelativeToMode = newCamRelMode;
                Iridium.Patches.AsyncPatchManager.UpdatePatchByTypeAsync(typeof(Iridium.Patches.JsonPatches.LegacyBehaviorPatch));
            }
        }

        private void DrawHitSoundAndJudgeTextTab()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("EnableHitSoundPitch"), UIUtils.LabelStyle);
            GUILayout.FlexibleSpace();
            bool newEnableHitSound = UIUtils.M3Switch(hitSound.enableHitSoundPitch, "");
            if (newEnableHitSound != hitSound.enableHitSoundPitch)
            {
                hitSound.enableHitSoundPitch = newEnableHitSound;
                Iridium.Patches.AsyncPatchManager.UpdatePatchByTypeAsync(typeof(Iridium.Patches.StdHitSoundPatch));
            }
            GUILayout.EndHorizontal();

            UILayout.DrawSettingGroupTitle(Localization.Get("JudgeTextSettings"));

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("JudgeTextSettings"), UIUtils.LabelStyle);
            GUILayout.FlexibleSpace();
            bool newEnableJudgeText = UIUtils.M3Switch(judgeText.enableJudgeTextCustomization, "");
            if (newEnableJudgeText != judgeText.enableJudgeTextCustomization)
            {
                judgeText.enableJudgeTextCustomization = newEnableJudgeText;
                Iridium.Patches.AsyncPatchManager.UpdatePatchByTypeAsync(typeof(Iridium.Patches.StdHitTextMeshInitPatch));
                Iridium.Patches.AsyncPatchManager.UpdatePatchByTypeAsync(typeof(Iridium.Patches.StdHitTextMeshShowPatch));
                Iridium.Patches.AsyncPatchManager.UpdatePatchByTypeAsync(typeof(Iridium.Patches.JudgeTextPatches.ResetTimingOnRewindPatch));
            }
            GUILayout.EndHorizontal();

            if (judgeText.enableJudgeTextCustomization)
            {
                GUILayout.Space(8);

                bool newShowAsOffset = UIUtils.M3Switch(judgeText.showAsOffset, Localization.Get("ShowAsOffset"));
                if (newShowAsOffset != judgeText.showAsOffset)
                {
                    judgeText.showAsOffset = newShowAsOffset;
                    Iridium.Patches.AsyncPatchManager.UpdatePatchByTypeAsync(typeof(Iridium.Patches.StdHitTextMeshShowPatch));
                }

                GUILayout.Space(8);

                GUI.enabled = !judgeText.showAsOffset;

                GUILayout.Label(Localization.Get("CustomJudgeText"), UIUtils.LabelStyle);
                GUILayout.Space(4);

                DrawJudgeTextInput("TooEarly", ref judgeText.tooEarly);
                DrawJudgeTextInput("VeryEarly", ref judgeText.veryEarly);
                DrawJudgeTextInput("EarlyPerfect", ref judgeText.earlyPerfect);
                DrawJudgeTextInput("Perfect", ref judgeText.perfect);
                DrawJudgeTextInput("LatePerfect", ref judgeText.latePerfect);
                DrawJudgeTextInput("VeryLate", ref judgeText.veryLate);
                DrawJudgeTextInput("TooLate", ref judgeText.tooLate);
                DrawJudgeTextInput("Multipress", ref judgeText.multipress);
                DrawJudgeTextInput("FailMiss", ref judgeText.failMiss);
                DrawJudgeTextInput("FailOverload", ref judgeText.failOverload);

                GUI.enabled = true;

                GUILayout.Space(8);
                if (GUILayout.Button(Localization.Get("ResetJudgeText"), UIUtils.ButtonStyle, GUILayout.Width(120)))
                {
                    judgeText.ResetToDefault();
                }
            }
        }

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }

        private void DrawPatchSystemTab()
        {
            UILayout.DrawSettingGroupTitle(Localization.Get("PatchModeSettings"));

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("UseILPatch"), UIUtils.LabelStyle);
            GUILayout.FlexibleSpace();
            bool newUseIL = UIUtils.M3Switch(patchMode.useILPatch, "");
            if (newUseIL != patchMode.useILPatch)
            {
                patchMode.useILPatch = newUseIL;
                Iridium.Core.BasePatchMethod.SyncILModeFromSettings();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUILayout.Label(
                patchMode.useILPatch
                    ? Localization.Get("TranspilerModeDescription")
                    : Localization.Get("PrefixPostfixModeDescription"),
                UIUtils.LabelSecondaryStyle);

            GUILayout.Space(16);
            UILayout.DrawSettingGroupTitle("Info");
            GUILayout.Label(
                "Patches using StdPatchMethod: HitSound, AutoplayText, BPM, " +
                "PendingTweens, Filters, JudgeText, MoveFloor, RecolorFloor, " +
                "DecoManager, HideWatermark, ForceDifficultyUI",
                UIUtils.LabelSecondaryStyle);
        }

        private void DrawJudgeTextInput(string key, ref string value)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(24));
            GUILayout.Label(Localization.Get($"JudgeText_{key}"), UIUtils.LabelStyle, GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            string newValue = GUILayout.TextField(value, 20, UIUtils.TextFieldStyle, GUILayout.Width(120));
            if (newValue != value)
            {
                value = newValue;
            }
            GUILayout.EndHorizontal();
        }

    }
}
