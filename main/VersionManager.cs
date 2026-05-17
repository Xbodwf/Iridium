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
        public static VersionType Type => VersionType.Release;
        public const int MinorVersion = 0;

        public static string GetFullVersionString()
        {
            string baseVersion = Main.Mod?.Info.Version ?? "Error";
            string adofaiSuffix = $"+adofai{BuildInfo.AdofaiVersion}";
            if (Type == VersionType.Release)
            {
                return $"{baseVersion}{adofaiSuffix}";
            }
            return $"{baseVersion}_{Type.ToString().ToLower()}{MinorVersion}{adofaiSuffix}";
        }
    }
}
