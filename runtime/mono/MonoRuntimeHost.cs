namespace Iridium.Runtime
{
    public sealed class MonoRuntimeHost : IRuntimeHost
    {
        public RuntimeKind Runtime => RuntimeKind.Mono;
        public IPatchBackend PatchBackend { get; } = new MonoHarmonyBackend();

        public void Initialize(string ownerId)
        {
            PatchBackend.Initialize(ownerId);
        }

        public void Dispose()
        {
            PatchBackend.Dispose();
        }
    }
}
