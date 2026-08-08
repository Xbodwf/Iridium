namespace Iridium.Modules.AsyncInputOptimize
{
    public static class AsyncInputData
    {
        public static long currFrameTick;
        public static long prevFrameTick;
        public static long offsetTick;
        public static long offsetTick_REAL;
        public static long[] offsetTicks = new long[30];
        public static int offsetTicksIndex;
        public static double dspTime;
    }
}
