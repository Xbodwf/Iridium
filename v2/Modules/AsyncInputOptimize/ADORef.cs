using HarmonyLib;
using System;
using System.Reflection;

namespace Iridium.Modules.AsyncInputOptimize
{
    public static class ADORef_scrConductor
    {
        public static readonly Type @this = typeof(scrConductor);
        public static readonly FieldInfo dspTimeSong = @this.GetField(nameof(dspTimeSong), AccessTools.all);
        public static readonly FieldInfo previousFrameTime = @this.GetField(nameof(previousFrameTime), AccessTools.all);
        public static readonly FieldInfo lastReportedPlayheadPosition = @this.GetField(nameof(lastReportedPlayheadPosition), AccessTools.all);
    }
}
