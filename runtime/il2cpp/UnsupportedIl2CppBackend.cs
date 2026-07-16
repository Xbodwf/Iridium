namespace Iridium.Runtime
{
    /// <summary>
    /// Safe fallback until the IL2CPP loader supplies a concrete backend.
    /// </summary>
    public sealed class UnsupportedIl2CppBackend : IIl2CppPatchBackend
    {
        public RuntimeKind Runtime => RuntimeKind.Il2Cpp;
        public bool IsReady => false;

        public void Initialize(string ownerId)
        {
        }

        public void Attach(IIl2CppRuntimeApi runtimeApi)
        {
        }

        public void SetPerformanceMode(bool useTranspiler)
        {
        }

        public PatchResult Apply(string patchId, object definition) =>
            PatchResult.Failed($"IL2CPP backend is unavailable for '{patchId}'.");

        public PatchResult Remove(string patchId, object definition) =>
            PatchResult.Removed($"IL2CPP patch '{patchId}' was not loaded.");

        public void RemoveAll()
        {
        }

        public void Dispose()
        {
        }
    }
}
