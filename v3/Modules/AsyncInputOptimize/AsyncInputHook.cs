using System.Diagnostics;
using UnityEngine;

using static Iridium.Modules.AsyncInputOptimize.ADORef_scrConductor;

namespace Iridium.Modules.AsyncInputOptimize
{
    public static class AsyncInputHook
    {
        public static void ResetTime()
        {
            // SafeDSPTime.SetOffset(0);
            AsyncInputData.prevFrameTick = AsyncInputData.currFrameTick;
            AsyncInputData.currFrameTick = AsyncInputTime.GetDateTimePreciseAsFileTime();
            AsyncInputData.offsetTick = AsyncInputData.currFrameTick - (ulong)SafeDSPTime.InterpolationDSPTimeAsFileTime;
            AsyncInputData.dspTime = SafeDSPTime.InterpolationDSPTime;
        }
        public static void PauseTime()
        {
            AsyncInputData.prevFrameTick = AsyncInputData.currFrameTick;
            AsyncInputData.currFrameTick = AsyncInputTime.GetDateTimePreciseAsFileTime();
            AsyncInputData.offsetTick = AsyncInputData.currFrameTick - (ulong)SafeDSPTime.InterpolationDSPTimeAsFileTime;
            AsyncInputData.dspTime = SafeDSPTime.InterpolationDSPTime;
        }
        public static void ConductorUpdate(scrConductor @this)
        {
            int counter = 0;
        JMP_RELOAD:
            double dspTime = SafeDSPTime.InterpolationDSPTime;
            double time = Time.unscaledTimeAsDouble;
            @this.dspTime = dspTime;
            lastReportedPlayheadPosition.SetValue(@this, dspTime);
            previousFrameTime.SetValue(@this, time);
            if (AsyncInputManager.isActive)
            {
                if (scrController.instance?.paused ?? true)
                {
                    PauseTime();
                    return;
                }
                double audio_precise = SafeDSPTime.GetAuidoPrecise();
                AsyncInputData.prevFrameTick = AsyncInputData.currFrameTick;
                AsyncInputData.currFrameTick = AsyncInputTime.GetDateTimePreciseAsFileTime();
                AsyncInputData.dspTime = (AsyncInputData.currFrameTick - AsyncInputData.offsetTick) / 10000000.0;
                AsyncInputData.offsetTick_REAL = AsyncInputData.currFrameTick - (ulong)SafeDSPTime.InterpolationDSPTimeAsFileTime;
                AsyncInputData.offsetTicks[AsyncInputData.offsetTicksIndex++] = AsyncInputData.offsetTick_REAL;
                long delta = (long)AsyncInputData.offsetTick_REAL - (long)AsyncInputData.offsetTick;

                if (System.Math.Abs(delta) > audio_precise * (10000000 << 2) && audio_precise != 0)
                {
                    AsyncInputData.offsetTicksIndex = 0;
                    AsyncInputData.offsetTick += (ulong)delta;
                    Iridium.Main.Logger?.Warning("[AsyncInputOptimize] DSPTime XRUN Error");
                    counter++;
                    if (counter < 128)
                        goto JMP_RELOAD;
                }
                if (AsyncInputData.offsetTicksIndex == 30)
                {
                    AsyncInputData.offsetTicksIndex = 0;
                    ulong datas = 0;
                    foreach (ulong val in AsyncInputData.offsetTicks)
                        datas += val - AsyncInputTime.QPC_BIAS;
                    datas = datas / 30 + AsyncInputTime.QPC_BIAS;
                    delta = (long)datas - (long)AsyncInputData.offsetTick;
                    if (System.Math.Abs(delta) > audio_precise * (10000000 >> 1))
                    {
                        AsyncInputData.offsetTick += (ulong)delta;
                        Iridium.Main.Logger?.Log("[AsyncInputOptimize] Offset fix");
                    }
                }

                AsyncInputManager.prevFrameTick = AsyncInputData.prevFrameTick;
                AsyncInputManager.currFrameTick = AsyncInputData.currFrameTick;
                AsyncInputManager.offsetTick = AsyncInputData.offsetTick;
                AsyncInputManager.previousFrameTime = Time.timeAsDouble;
                AsyncInputManager.offsetTickUpdated = true;

                scrController ctrl = ADOBase.controller;
                if (ctrl != null && !ctrl.paused)
                    ctrl.UpdateInput();
            }
            @this.prev_dspTime = @this.dspTime;
            @this.prev_unityDspTime = dspTime;
        }
    }
}
