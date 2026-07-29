using UnityEngine;

namespace Iridium.Modules.AsyncInputOptimize
{
    public static class Main
    {
        public static void Enable()
        {
            AudioSettings.Reset(AudioSettings.GetConfiguration());
        }
        public static void Disable()
        {
            SafeDSPTime.Destroy();
        }
    }
}
