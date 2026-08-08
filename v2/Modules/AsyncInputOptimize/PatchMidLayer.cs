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
            // releaseNumber <= 110 (R110): no PlatformHelper update needed
            if (GameVersion.IsR136OrLater)
            {
                if (scrConductor.isAudioOutputDeviceChanged)
                {
                    scrController.CheckForAudioOutputChange();
                    scrConductor.isAudioOutputDeviceChanged = false;
                }
                PlatformHelper.Instance.Update();
            }
            AsyncInputHook.ConductorUpdate(@this);
        }
    }
}
