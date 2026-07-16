using System;

namespace Iridium.Runtime
{
    /// <summary>
    /// Runtime-specific hook implementation. Patch definitions must not depend
    /// on Harmony or on an IL2CPP library directly.
    /// </summary>
    public interface IPatchBackend : IDisposable
    {
        RuntimeKind Runtime { get; }
        bool IsReady { get; }

        void Initialize(string ownerId);
        void SetPerformanceMode(bool useTranspiler);
        PatchResult Apply(string patchId, object definition);
        PatchResult Remove(string patchId, object definition);
        void RemoveAll();
    }
}
