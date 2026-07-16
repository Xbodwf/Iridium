using System;
using System.Collections.Generic;

namespace Iridium.Runtime
{
    /// <summary>
    /// IL2CPP backend independent of any particular mobile Loader. The Loader
    /// supplies metadata resolution and native detours through IIl2CppRuntimeApi.
    /// </summary>
    public sealed class Il2CppPatchBackend : IIl2CppPatchBackend
    {
        private readonly Dictionary<string, IIl2CppHookHandle> _hooks = new();
        private IIl2CppRuntimeApi? _runtimeApi;
        private bool _useTranspiler;

        public RuntimeKind Runtime => RuntimeKind.Il2Cpp;
        public bool IsReady => _runtimeApi?.IsInitialized == true;

        public void Initialize(string ownerId)
        {
            // IL2CPP hook ownership is managed by the Loader API.
        }

        public void Attach(IIl2CppRuntimeApi runtimeApi)
        {
            _runtimeApi = runtimeApi ?? throw new ArgumentNullException(nameof(runtimeApi));
        }

        public void SetPerformanceMode(bool useTranspiler)
        {
            // IL2CPP has no Mono CIL transpiler. Performance mode is expressed
            // by the supplied replacement/detour definition instead.
            _useTranspiler = useTranspiler;
        }

        public PatchResult Apply(string patchId, object definition)
        {
            if (!IsReady)
                return PatchResult.Failed("IL2CPP runtime API is not initialized.");

            if (!(definition is Il2CppPatchDefinition patch))
                return PatchResult.Failed($"'{patchId}' is not an IL2CPP patch definition.");

            if (_hooks.ContainsKey(patchId))
                return PatchResult.Applied($"IL2CPP patch '{patchId}' is already applied.");

            var method = _runtimeApi!.ResolveMethod(patch.Request.Target);
            if (method == null)
                return PatchResult.NotFound(patch.Request.Target.ToString());

            try
            {
                var hook = _runtimeApi.InstallHook(method, patch.Request);
                if (hook == null)
                    return PatchResult.Failed($"Hook installation returned null for '{patchId}'.");

                _hooks.Add(patchId, hook);
                return PatchResult.Applied(patch.Request.Target.ToString());
            }
            catch (Exception error)
            {
                return PatchResult.Failed(error.Message);
            }
        }

        public PatchResult Remove(string patchId, object definition)
        {
            if (!_hooks.TryGetValue(patchId, out var hook))
                return PatchResult.Removed($"IL2CPP patch '{patchId}' was not applied.");

            try
            {
                hook.Dispose();
                _hooks.Remove(patchId);
                return PatchResult.Removed(patchId);
            }
            catch (Exception error)
            {
                return PatchResult.Failed(error.Message);
            }
        }

        public void RemoveAll()
        {
            foreach (var hook in _hooks.Values)
                hook.Dispose();
            _hooks.Clear();
        }

        public void Dispose()
        {
            RemoveAll();
            _runtimeApi = null;
        }
    }
}
