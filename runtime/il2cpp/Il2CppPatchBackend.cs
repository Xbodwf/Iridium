using System;
using System.Collections.Generic;

namespace Iridium.Runtime
{
    public sealed class Il2CppMethodDescriptor
    {
        public string TypeName { get; }
        public string MethodName { get; }
        public string Signature { get; }

        public Il2CppMethodDescriptor(string typeName, string methodName, string signature = "")
        {
            TypeName = typeName;
            MethodName = methodName;
            Signature = signature;
        }

        public override string ToString() =>
            string.IsNullOrEmpty(Signature)
                ? $"{TypeName}.{MethodName}"
                : $"{TypeName}.{MethodName}{Signature}";
    }

    public interface IIl2CppMethodHandle
    {
        Il2CppMethodDescriptor Descriptor { get; }
    }

    public interface IIl2CppHookHandle : IDisposable
    {
        IIl2CppMethodHandle Method { get; }
    }

    public sealed class Il2CppHookRequest
    {
        public Il2CppMethodDescriptor Target { get; }
        public Delegate? Prefix { get; }
        public Delegate? Postfix { get; }
        public Delegate? Replacement { get; }

        public Il2CppHookRequest(
            Il2CppMethodDescriptor target,
            Delegate? prefix = null,
            Delegate? postfix = null,
            Delegate? replacement = null)
        {
            Target = target;
            Prefix = prefix;
            Postfix = postfix;
            Replacement = replacement;
        }
    }

    /// <summary>
    /// Supplied by the IL2CPP Loader. It owns metadata lookup and native detour
    /// mechanics; this project deliberately has no dependency on a hook library.
    /// </summary>
    public interface IIl2CppRuntimeApi
    {
        bool IsInitialized { get; }
        IIl2CppMethodHandle? ResolveMethod(Il2CppMethodDescriptor descriptor);
        IIl2CppHookHandle InstallHook(IIl2CppMethodHandle method, Il2CppHookRequest request);
    }

    public interface IIl2CppPatchBackend : IPatchBackend
    {
        void Attach(IIl2CppRuntimeApi runtimeApi);
    }

    public sealed class Il2CppPatchDefinition
    {
        public Il2CppHookRequest Request { get; }

        public Il2CppPatchDefinition(Il2CppHookRequest request)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
        }
    }
}
