using System.Reflection;

namespace Iridium.Modules.AsyncInputOptimize
{
    /// <summary>
    /// 运行时检测 ADOFAI 版本号（GCNS.releaseNumber）。
    /// 用反射读取，避免 const 编译期内联导致的多版本不兼容。
    /// </summary>
    internal static class GameVersion
    {
        private static readonly int _releaseNumber;

        static GameVersion()
        {
            try
            {
                var field = typeof(GCNS).GetField("releaseNumber", BindingFlags.Public | BindingFlags.Static);
                _releaseNumber = field != null ? (int)field.GetValue(null) : 0;
            }
            catch
            {
                _releaseNumber = 0;
            }
        }

        /// <summary>RELEASE_2_5_0_R110</summary>
        public static bool IsR110 => _releaseNumber == 110 || _releaseNumber <= 110;

        /// <summary>Alpha_2_9_8_R136 / ALPHA_2_9_8_R136</summary>
        public static bool IsR136OrLater => _releaseNumber >= 136;
    }
}
