namespace Iridium.Modules.AsyncInputOptimize
{
    public static class AsyncInputData
    {
        public static ulong currFrameTick;
        public static ulong prevFrameTick;
        public static ulong offsetTick;
        public static ulong offsetTick_REAL;
        public static ulong[] offsetTicks = new ulong[30];
        public static int offsetTicksIndex;
        public static double dspTime;
    }
}
