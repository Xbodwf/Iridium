namespace Iridium.Modules.FerriteCore
{
    /// <summary>
    /// FerriteCore-style configuration, loaded from the standalone
    /// <c>Config/FerriteCore.json</c> file. This file is
    /// deliberately kept OUT of Settings.xml — Settings.xml only carries
    /// the 基础优化 master switch (<see cref="Config.MemorySettings.enableBasicOptimization"/>).
    ///
    /// The defaults below ARE the “basic profile” applied by that switch:
    /// safe, low-risk tuning only. Framerate / vsync / audio / L1 renderer
    /// sleep stay opt-in via this file for advanced users.
    /// </summary>
    public class FerriteConfig
    {
        /// <summary>2 = basic-profile defaults (incremental GC, scene-switch GC, shadow cap).</summary>
        public int configVersion = 2;

        public L0Settings L0 { get; set; } = new();
        public L1Settings L1 { get; set; } = new();
    }

    /// <summary>L0: engine-level tuning (no Harmony on game methods).</summary>
    public class L0Settings
    {
        public bool enableEngineTune = true;

        public int targetFrameRate = 0;
        public bool forceVSyncCount = false;
        public int vsyncCount = 0;

        public bool enableQualityPreset = false;
        public int qualityPreset = 0;

        public bool limitShadowDistance = true;
        public float shadowDistance = 30f;

        public bool enableIncrementalGC = true;
        public bool gcOnSceneSwitch = true;

        public bool limitFixedTimestep = false;
        public float fixedTimestep = 0.01f;
        public bool limitMaxAllowedTimestep = false;
        public float maxAllowedTimestep = 0.1f;

        public bool tuneAudioBuffer = false;
        public int audioBufferSize = 512;
    }

    /// <summary>L1: soft optimizations (renderer sleep).</summary>
    public class L1Settings
    {
        public bool enableL1 = false;

        public bool sleepOffscreenRenderers = false;
        public float sleepDistance = 30f;
    }
}
