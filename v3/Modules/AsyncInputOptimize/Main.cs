using UnityEngine;

namespace Iridium.Modules.AsyncInputOptimize
{
    public static class Main
    {
        public static void Enable()
        {
            SafeDSPTime.Init();
            AudioSettings.OnAudioConfigurationChanged += SafeDSPTime.Init;
        }
        public static void Disable()
        {
            AudioSettings.OnAudioConfigurationChanged -= SafeDSPTime.Init;
            SafeDSPTime.Destroy();
        }
    }
}
