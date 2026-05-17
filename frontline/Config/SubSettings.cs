using UnityEngine;

namespace Iridium.Config
{
    public class OptimizerSettings
    {
        public bool enableOptimizer = false;
        public bool optimizeMoveTrack = false;
        public bool optimizeRecolorTrack = false;
        public bool optimizeFilters = false;
        public bool optimizeCLSAsyncScan = false;
        public double divideBy = 1.0;
        public bool dontShowSavedMemory = false;
        public bool dontCompress = false;
        public bool dontResizeMultipleOf4 = false;
        public bool dontResizeCollider = false;
        public bool disableShadows = false;
        public bool optimizeDecorationUpdate = false;
        public bool optimizeTileUpdate = false;
        public bool fastLoading = false;
        public bool skipEventIfPaused = false;
        public bool optimizeEventIcons = false;
        public bool optimizeScnGameUpdate = false;
        public bool optimizeMoveDecorations = false;
        public bool optimizeFloorMesh = false;
        public bool optimizeFfxDecorations = false; // 新增：优化 ffx 装饰物更新
        public bool optimizeDecorationAnimation = false; // 新增：装饰物逐帧动画优化 (RTMaxSize)

        // Particle Optimization Patches (粒子优化)
        public bool optimizeParticle = false;             // 主开关
        public bool optimizeParticleInactive = false;     // 跳过非活跃粒子 + 对象池
        public bool optimizeParticleCulling = false;      // 离屏暂停模拟
        public bool optimizeParticleLod = false;          // LOD 跳过

        // Scene Optimization Patches
        public bool cacheGameObjectReferences = false;
        public bool optimizeEventProcessing = false;
        public bool optimizeEditorMouseDetection = false;
        public bool optimizeEditorEventIndicators = false;

        // Loading Optimization Patches
        public bool cacheFloorEvents = false;
        public bool optimizeMoveTrackTweens = false;
        public bool batchMoveDecorations = false;

        // DOTween Optimization Patches
        public bool optimizeDOTweenGlobal = false;
        public int dotweenTweenerCapacity = 500;
        public int dotweenSequenceCapacity = 100;
        public bool dotweenDefaultRecyclable = true;
        public bool dotweenDisableSafeMode = false;

        // Extreme Optimization Patches (极端情况优化)
        public bool enableExtremeOptimization = false; // 启用极端优化（分帧处理）
        public int maxTweensPerFrame = 100; // 每帧最多创建的Tween数量

        // Frame-Spread Loading (分帧加载)
        public bool frameSpreadDecorationLoading = false; // 启用装饰物分帧加载
        public int decorationsPerFrame = 50; // 每帧加载的装饰物数量
    }

    public class UISettings
    {
        public bool removeNews = false;
        public bool hideBetaWatermark = false;
        public bool moveAutoplayText = false;
        public float autoplayTextX = 0f;
        public float autoplayTextY = 0f;
        public bool forceDifficultyUI = false;
        public bool enableCircleArc = false;
        public bool alwaysCountdown = false;
    }

    public class LobbyMusicSettings
    {
        public bool enableLobbyMusicPatch = false;
        public bool enableCustomBpm = false;
        public float customBpm = 120f;
        public bool fastMusic = true;
        public bool customMusic = false;
        public string defaultMusicPath = string.Empty;
        public string fastMusicPath = string.Empty;
    }

    public class MemorySettings
    {
        public bool enableMemoryOptimization = false;
        public bool enableSmartGC = false;
        public float gcInterval = 60f;
        public bool gcInGame = false;
    }

    public class CompatibilitySettings
    {
        public bool enableLegacyPauseFix = false;
        public bool enableNoFailTooEarly = false;
        public bool forceAngleData = false;
        public bool scaleFilterSpeedWithPitch = false;
        public LegacyBehaviorMode legacyFlashMode = LegacyBehaviorMode.Default;
        public LegacyBehaviorMode legacyCamRelativeToMode = LegacyBehaviorMode.Default;
    }

    public enum LegacyBehaviorMode
    {
        Default,
        AlwaysOff,
        AlwaysOn
    }

    public class HitSoundSettings
    {
        public bool enableHitSoundPitch = false;
    }

    public class JudgeTextSettings
    {
        public bool enableJudgeTextCustomization = false;
        public bool showAsOffset = false; // 显示为偏移 (如 "5ms")
        
        // 自定义判定文本
        public string tooEarly = "TooEarly";
        public string veryEarly = "VeryEarly";
        public string earlyPerfect = "EarlyPerfect";
        public string perfect = "Perfect";
        public string latePerfect = "LatePerfect";
        public string veryLate = "VeryLate";
        public string tooLate = "TooLate";
        public string multipress = "Multipress";
        public string failMiss = "FailMiss";
        public string failOverload = "FailOverload";
        
        public string GetTextForHitMargin(int hitMargin)
        {
            return hitMargin switch
            {
                0 => tooEarly,
                1 => veryEarly,
                2 => earlyPerfect,
                3 => perfect,
                4 => latePerfect,
                5 => veryLate,
                6 => tooLate,
                7 => multipress,
                8 => failMiss,
                9 => failOverload,
                _ => ""
            };
        }
        
        public void ResetToDefault()
        {
            tooEarly = "TooEarly";
            veryEarly = "VeryEarly";
            earlyPerfect = "EarlyPerfect";
            perfect = "Perfect";
            latePerfect = "LatePerfect";
            veryLate = "VeryLate";
            tooLate = "TooLate";
            multipress = "Multipress";
            failMiss = "FailMiss";
            failOverload = "FailOverload";
        }
    }

    /// <summary>
    /// 补丁模式设置 — 控制 Transpiler / PrefixPostfix 切换
    /// </summary>
    public class PatchModeSettings
    {
        /// <summary>
        /// true = 使用 Transpiler（IL注入，性能优先）
        /// false = 使用 Prefix/Postfix（兼容性优先）
        /// </summary>
        public bool useILPatch = false;
    }
}
