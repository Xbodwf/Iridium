namespace Iridium.Modules.FerriteCore
{
    /// <summary>
    /// FerriteCore-style configuration, loaded from the standalone
    /// <c>Config/FerriteCore.json</c> file. This file is
    /// deliberately kept OUT of Settings.xml — the XML settings only carry
    /// the master enable/disable switch.
    /// </summary>
    public class FerriteConfig
    {
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

        public bool limitShadowDistance = false;
        public float shadowDistance = 50f;

        public bool enableIncrementalGC = false;
        public bool gcOnSceneSwitch = false;

        public bool limitFixedTimestep = false;
        public float fixedTimestep = 0.01f;
        public bool limitMaxAllowedTimestep = false;
        public float maxAllowedTimestep = 0.1f;

        public bool reducePhysicsQueries = false;

        public bool tuneAudioBuffer = false;
        public int audioBufferSize = 512;
    }

    /// <summary>L1: soft optimizations (renderer sleep, string pool).</summary>
    public class L1Settings
    {
        public bool enableL1 = false;

        public bool sleepOffscreenRenderers = false;
        public float sleepDistance = 30f;

        public bool enableStringPool = false;

        public bool enableOwnPool = false;
    }

    public enum QualityPreset
    {
        Performance,
        Balanced,
        Quality
    }
}
