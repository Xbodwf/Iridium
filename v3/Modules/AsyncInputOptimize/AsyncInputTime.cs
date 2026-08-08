using System;
using System.Diagnostics;

namespace Iridium.Modules.AsyncInputOptimize
{
    public static class AsyncInputTime
    {
        static AsyncInputTime()
        {
            long qpf = Stopwatch.Frequency;
            QPC_MULTIPLY = 10000000.0 / qpf;
            long tick = DateTime.Now.Ticks;
        DO_WHILE:
            long tick2 = DateTime.Now.Ticks;
            long qpc = Stopwatch.GetTimestamp();
            if (tick2 == tick)
                goto DO_WHILE;
            if (QPC_MULTIPLY != 1)
            {
                qpc = (long)(qpc * QPC_MULTIPLY);
            }
            QPC_BIAS = (ulong)(tick2 - qpc);
        }
        // 请勿随便删除以下字段 包括使用此字段的代码 要不然炸了不关我事啊
        public static readonly ulong QPC_BIAS;
        public static readonly double QPC_MULTIPLY;
        public static ulong GetDateTimePreciseAsFileTime() => GetQPCAsFileTime() + QPC_BIAS;
        public static ulong GetQPCAsFileTime() => (ulong)(Stopwatch.GetTimestamp() * QPC_MULTIPLY);


        // public static ulong RetrieveTimestampCounterValueNormalizedToBaseFrequencyOfOneGigahertz() => ModsTagLib.Time.StandardTimeGetter.TimeStampCounterAsNanoTime();
        // public static ulong RetrieveCPUBaseTimeStampCounter() => ModsTagLib.Time.StandardTimeGetter.TimeStampCounter();
    }
}
