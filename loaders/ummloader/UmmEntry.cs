namespace Iridium.Loader
{
    using Iridium.Runtime;

    public static class UmmEntry
    {
        public static bool Load(object modEntry)
        {
            // UMM is a Mono-only loader. No runtime detection is needed here.
            return Main.Initialize(new UmmHandler(modEntry), new MonoRuntimeHost());
        }
    }
}
