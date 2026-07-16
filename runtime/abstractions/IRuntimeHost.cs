namespace Iridium.Runtime
{
    public interface IRuntimeHost : System.IDisposable
    {
        RuntimeKind Runtime { get; }
        IPatchBackend PatchBackend { get; }

        void Initialize(string ownerId);
    }
}
