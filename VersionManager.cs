using System;

namespace Iridium
{
    public enum VersionType
    {
        Hotfix,
        Release,
        Beta,
        NightlyBeta,
        Prerelease
    }

    public static class VersionManager
    {
        public static VersionType Type => VersionType.NightlyBeta;
        public const int MinorVersion = 4;

        public static string GetFullVersionString()
        {
            string baseVersion = Main.Mod?.Info.Version ?? "Error";
            if (Type == VersionType.Release)
            {
                return baseVersion;
            }
            return $"{baseVersion}-{Type.ToString().ToLower()}{MinorVersion}";
        }
    }
}
