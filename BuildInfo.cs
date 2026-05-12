namespace Iridium;

public static class BuildInfo
{
    /// <summary>
    /// Hardcoded ADOFAI version this build targets.
    /// Set by the build script / CI for each variant.
    /// </summary>
#if ADOFAI_2_10_0
    public const string AdofaiVersion = "2.10.0";
#else
    public const string AdofaiVersion = "2.9.8";
#endif
}
