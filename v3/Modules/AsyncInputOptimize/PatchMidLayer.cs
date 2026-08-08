using ADOFAI.Common.Platform;

namespace Iridium.Modules.AsyncInputOptimize
{
    public static class PatchMidLayer
    {
        public static void Reset()
        {
            AsyncInputHook.ResetTime();
        }
        public static void StartOrPlay()
        {
            AsyncInputHook.ResetTime();
        }
        public static void ConductorUpdate(scrConductor @this)
        {
            if (scrConductor.isAudioOutputDeviceChanged)
            {
                scrController.CheckForAudioOutputChange();
                scrConductor.isAudioOutputDeviceChanged = false;
            }
            PlatformHelper.instance.Update();
            AsyncInputHook.ConductorUpdate(@this);
        }
    }
}
