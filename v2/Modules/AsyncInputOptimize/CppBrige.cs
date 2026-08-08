using System.Diagnostics;

namespace Iridium.Modules.AsyncInputOptimize
{
    public static class CppBrige
    {
        public static long GetSystemTick() => Stopwatch.GetTimestamp();
    }
}
