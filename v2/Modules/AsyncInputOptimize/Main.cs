namespace Iridium.Modules.AsyncInputOptimize
{
    public static class Main
    {
        public static void Enable()
        {
            SafeDSPTime.Init();
        }
        public static void Disable()
        {
            SafeDSPTime.Destroy();
        }
    }
}
