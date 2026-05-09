using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Iridium.Core
{
    /// <summary>
    /// 补丁的类型
    /// </summary>
    [Flags]
    public enum PatchTypes
    {
        NONE = 0,
        Prefix = 1,
        Postfix = 2,
        PP = 3,
    }

    /// <summary>
    /// 所有补丁方法的基类<br/>
    /// </summary>
    public abstract class BasePatchMethod
    {
        public static List<BasePatchMethod> _methods = new(16);

        public BasePatchMethod(PatchTypes t)
        {
            type = t;
            // 从设置中读取 IL 模式
            il = Main.Settings?.patchMode.useILPatch ?? false;
            lock (_methods)
            {
                _methods.Add(this);
                id = _methods.Count - 1;
            }
        }

        /// <summary>补丁类型, 不可修改</summary>
        public readonly PatchTypes type;

        /// <summary>补丁的唯一识别码</summary>
        protected internal readonly int id;

        /// <summary>
        /// false = 使用 Prefix/Postfix (兼容性优先)<br/>
        /// true = 使用 Transpiler (性能优先)
        /// </summary>
        public volatile bool il;

        /// <summary>当前已注册到 Harmony 的补丁方法</summary>
        internal MethodInfo? patchedResult;

        /// <summary>获取需要修补的目标方法</summary>
        public abstract MethodBase GetTargetMethod();

        /// <summary>将补丁注册到 Harmony</summary>
        public abstract void StartPatch();

        /// <summary>从 Harmony 移除补丁</summary>
        public abstract void StopPatch();

        /// <summary>
        /// 运行时切换补丁模式
        /// </summary>
        public void SetILMode(bool useIL)
        {
            if (useIL == il) return;
            il = useIL;
            StopPatch();
            StartPatch();
        }

        /// <summary>当前是否已打上补丁</summary>
        public bool IsPatched => patchedResult != null;

        /// <summary>
        /// 将所有已注册的 BasePatchMethod 的 IL 模式与设置同步。
        /// 如果某个实例已打上补丁且 il 与设置不同，则运行时切换。
        /// </summary>
        public static void SyncILModeFromSettings()
        {
            bool useIL = Main.Settings?.patchMode.useILPatch ?? false;
            lock (_methods)
            {
                foreach (var m in _methods)
                {
                    if (m.IsPatched && m.il != useIL)
                    {
                        m.SetILMode(useIL);
                    }
                    else
                    {
                        m.il = useIL;
                    }
                }
            }
        }

        /// <summary>
        /// 强制重置内部状态（用于 PatchManager UnpatchAll 时清理）
        /// </summary>
        internal virtual void ForceReset()
        {
            patchedResult = null;
        }
    }

    /// <summary>
    /// 标准补丁方法抽象类<br/>
    /// T: 方法所属类型<br/>
    /// Res: 方法返回值类型 (void 可用 object)<br/>
    /// 只需实现 <see cref="Method(object[])"/>，即可自动支持 Prefix/Postfix 和 Transpiler 两种模式
    /// </summary>
    public abstract class StdPatchMethod<T, Res> : BasePatchMethod
    {
        public StdPatchMethod(PatchTypes t) : base(t)
        {
        }

        /// <summary>补丁执行后的返回值 (供 Postfix 读取/修改)</summary>
        public Res? result;

        /// <summary>补丁被执行时所在的目标实例</summary>
        public T? instance;

        /// <summary>
        /// 核心补丁逻辑。写一次，Prefix/Postfix 和 Transpiler 模式共用。
        /// </summary>
        /// <param name="args">原始方法的参数列表</param>
        /// <returns>false 则跳过原方法 (Prefix 语义), true 则继续执行</returns>
        public abstract bool Method(object[] args);

        // ============================================================
        //  Transpiler — 高性能模式
        //  直接修改目标方法的 IL，在 IL 层面注入 Method() 调用
        // ============================================================

        /// <summary>
        /// 根据 <see cref="BasePatchMethod.type"/> 自动生成对应 IL：
        /// <list type="bullet">
        ///   <item><term>Prefix</term><description>在方法开头注入 Method(args)，返回 false 则 ret</description></item>
        ///   <item><term>Postfix</term><description>在每个 ret 前注入 Method(args)，返回 false 则 pop + ret</description></item>
        ///   <item><term>PP</term><description>Prefix + Postfix 同时生效</description></item>
        /// </list>
        /// </summary>
        protected internal IEnumerable<CodeInstruction> IL(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase originalMethod)
        {
            if (type == 0 || (type & PatchTypes.PP) == 0)
                throw new Exception("你这不是Prefix的 又不是Postfix的 要几把干啥");

            // 必须先 ToList()，避免多次枚举导致后续遍历为空
            var instrList = instructions.ToList();
            var parameters = originalMethod.GetParameters();
            int paramCount = parameters.Length;
            bool isStatic = originalMethod.IsStatic;

            bool hasPrefix = (type & PatchTypes.Prefix) != 0;
            bool hasPostfix = (type & PatchTypes.Postfix) != 0;

            // ===========================
            //  1. Prefix 注入（方法开头）
            // ===========================
            if (hasPrefix)
            {
                // this.instance = __instance
                if (!isStatic)
                {
                    foreach (var ci in EmitStoreInstance(OpCodes.Ldarg_0))
                        yield return ci;
                }

                // object[] args = new object[paramCount]; args[0] = ...; args[1] = ...;
                var argsLocal = generator.DeclareLocal(typeof(object[]));
                foreach (var ci in EmitBuildArgs(argsLocal, parameters, isStatic))
                    yield return ci;

                // if (!patch.Method(args)) return false;
                var skipLabel = generator.DefineLabel();
                foreach (var ci in EmitCallMethod(argsLocal))
                    yield return ci;
                yield return new CodeInstruction(OpCodes.Brtrue_S, skipLabel);
                yield return new CodeInstruction(OpCodes.Ret);
                yield return new CodeInstruction(OpCodes.Nop).WithLabels(skipLabel);
            }

            // ===========================
            //  2. 遍历原始 IL 指令
            //     在每条 ret 前插入 Postfix
            // ===========================
            foreach (CodeInstruction ci in instrList)
            {
                if (hasPostfix && ci.opcode == OpCodes.Ret)
                {
                    // this.instance = __instance
                    if (!isStatic)
                    {
                        foreach (var ci2 in EmitStoreInstance(OpCodes.Ldarg_0))
                            yield return ci2;
                    }

                    // object[] args = new object[paramCount]; args[0] = ...
                    var postArgsLocal = generator.DeclareLocal(typeof(object[]));
                    foreach (var ci2 in EmitBuildArgs(postArgsLocal, parameters, isStatic))
                        yield return ci2;

                    // if (patch.Method(args)) goto original_ret; else pop & ret;
                    var postSkip = generator.DefineLabel();
                    foreach (var ci2 in EmitCallMethod(postArgsLocal))
                        yield return ci2;
                    yield return new CodeInstruction(OpCodes.Brtrue_S, postSkip);
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Ret);
                    ci.WithLabels(postSkip);
                }

                yield return ci;
            }

            yield break;

            // ---- 本地辅助函数 ----

            // Emit: patch.instance = value
            IEnumerable<CodeInstruction> EmitStoreInstance(OpCode loadValue)
            {
                yield return new CodeInstruction(OpCodes.Ldsfld,
                    typeof(BasePatchMethod).GetField(nameof(_methods))!);
                yield return new CodeInstruction(OpCodes.Ldc_I4, id);
                yield return new CodeInstruction(OpCodes.Call,
                    typeof(List<BasePatchMethod>).GetMethod("get_Item")!);
                yield return new CodeInstruction(OpCodes.Castclass, typeof(StdPatchMethod<T, Res>));
                yield return new CodeInstruction(loadValue);
                yield return new CodeInstruction(OpCodes.Stfld,
                    typeof(StdPatchMethod<T, Res>).GetField(nameof(instance))!);
            }

            // Emit: argsLocal[0] = arg0; argsLocal[1] = arg1; ...
            IEnumerable<CodeInstruction> EmitBuildArgs(LocalBuilder argsLocal, ParameterInfo[] parms, bool staticMethod)
            {
                yield return new CodeInstruction(OpCodes.Ldc_I4, paramCount);
                yield return new CodeInstruction(OpCodes.Newarr, typeof(object));
                yield return new CodeInstruction(OpCodes.Stloc, argsLocal.LocalIndex);

                for (int i = 0; i < paramCount; i++)
                {
                    yield return new CodeInstruction(OpCodes.Ldloc, argsLocal.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldc_I4, i);
                    yield return new CodeInstruction(OpCodes.Ldarg,
                        i + (staticMethod ? 0 : 1));
                    if (parms[i].ParameterType.IsValueType)
                        yield return new CodeInstruction(OpCodes.Box, parms[i].ParameterType);
                    yield return new CodeInstruction(OpCodes.Stelem_Ref);
                }
            }

            // Emit: _methods[id].Method(argsLocal)
            IEnumerable<CodeInstruction> EmitCallMethod(LocalBuilder argsLocal)
            {
                yield return new CodeInstruction(OpCodes.Ldsfld,
                    typeof(BasePatchMethod).GetField(nameof(_methods))!);
                yield return new CodeInstruction(OpCodes.Ldc_I4, id);
                yield return new CodeInstruction(OpCodes.Call,
                    typeof(List<BasePatchMethod>).GetMethod("get_Item")!);
                yield return new CodeInstruction(OpCodes.Castclass, typeof(StdPatchMethod<T, Res>));
                yield return new CodeInstruction(OpCodes.Ldloc, argsLocal.LocalIndex);
                yield return new CodeInstruction(OpCodes.Callvirt,
                    typeof(StdPatchMethod<T, Res>).GetMethod(nameof(Method),
                        BindingFlags.Instance | BindingFlags.Public)!);
            }
        }

        // ============================================================
        //  DynamicMethod — Harmony 兼容的静态方法桥接
        //  将 id 嵌入 DynamicMethod，从而在运行时可以从 _methods[id] 找到实例
        // ============================================================

        private DynamicMethod? _dynPrefix;
        private DynamicMethod? _dynPostfix;
        private DynamicMethod? _dynTranspiler;

        public override void StartPatch()
        {
            var target = GetTargetMethod();
            if (target == null)
            {
                Main.Logger?.Error($"[PatchMethod] GetTargetMethod() returned null for {GetType().Name}");
                return;
            }

            var harmony = Main.Harmony;
            if (harmony == null)
            {
                Main.Logger?.Error("[PatchMethod] Main.Harmony is null");
                return;
            }

            if (il)
            {
                // ======== Transpiler 模式 (性能优先) ========
                _dynTranspiler = BuildTranspiler();
                harmony.Patch(target, transpiler: new HarmonyMethod(_dynTranspiler));
                patchedResult = _dynTranspiler;
                Main.Logger?.Log($"[PatchMethod] {GetType().Name}[{id}] patched as Transpiler");
            }
            else
            {
                // ======== Prefix/Postfix 模式 (兼容性优先) ========
                HarmonyMethod? prefix = null;
                HarmonyMethod? postfix = null;

                if ((type & PatchTypes.Prefix) != 0)
                {
                    _dynPrefix = BuildPrefix();
                    prefix = new HarmonyMethod(_dynPrefix);
                }

                if ((type & PatchTypes.Postfix) != 0)
                {
                    _dynPostfix = BuildPostfix();
                    postfix = new HarmonyMethod(_dynPostfix);
                }

                harmony.Patch(target, prefix: prefix, postfix: postfix);
                patchedResult = _dynPrefix ?? _dynPostfix;
                Main.Logger?.Log($"[PatchMethod] {GetType().Name}[{id}] patched as Prefix/Postfix");
            }
        }

        public override void StopPatch()
        {
            var target = GetTargetMethod();
            if (target == null) return;

            var harmony = Main.Harmony;
            if (harmony == null) return;

            if (_dynPrefix != null)
            {
                harmony.Unpatch(target, _dynPrefix);
                _dynPrefix = null;
            }

            if (_dynPostfix != null)
            {
                harmony.Unpatch(target, _dynPostfix);
                _dynPostfix = null;
            }

            if (_dynTranspiler != null)
            {
                harmony.Unpatch(target, _dynTranspiler);
                _dynTranspiler = null;
            }

            patchedResult = null;
            Main.Logger?.Log($"[PatchMethod] {GetType().Name}[{id}] unpatched");
        }

        // ============================================================
        //  DynamicMethod 生成
        // ============================================================

        /// <summary>
        /// 生成 Prefix DynamicMethod<br/>
        /// Harmony 签名: bool Prefix(T __instance, object[] __args)  [instance]<br/>
        ///                bool Prefix(object[] __args)              [static]
        /// </summary>
        private DynamicMethod BuildPrefix()
        {
            var target = GetTargetMethod();
            bool isStaticTarget = target != null && target.IsStatic;

            var paramTypes = isStaticTarget
                ? new[] { typeof(object[]) }
                : new[] { typeof(T), typeof(object[]) };

            var dm = new DynamicMethod(
                $"__Iridium_Prefix_{id}",
                typeof(bool),
                paramTypes,
                typeof(StdPatchMethod<T, Res>).Module,
                true);

            var gen = dm.GetILGenerator();

            // var patch = (StdPatchMethod<T, Res>)_methods[id];
            EmitLoadInstance(gen);

            if (!isStaticTarget)
            {
                // patch.instance = __instance;
                gen.Emit(OpCodes.Dup);
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Stfld,
                    typeof(StdPatchMethod<T, Res>).GetField(nameof(instance),
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!);
                // __args is arg1
                gen.Emit(OpCodes.Ldarg_1);
            }
            else
            {
                // __args is arg0 (the only parameter)
                gen.Emit(OpCodes.Ldarg_0);
            }

            // return patch.Method(__args);
            gen.Emit(OpCodes.Callvirt,
                typeof(StdPatchMethod<T, Res>).GetMethod(nameof(Method),
                    BindingFlags.Instance | BindingFlags.Public)!);
            gen.Emit(OpCodes.Ret);

            return dm;
        }

        /// <summary>
        /// 生成 Postfix DynamicMethod<br/>
        /// Harmony 签名: void Postfix(T __instance, object[] __args, ref Res __result)  [instance]<br/>
        ///                void Postfix(object[] __args, ref Res __result)              [static]
        /// </summary>
        private DynamicMethod BuildPostfix()
        {
            var resultField = typeof(StdPatchMethod<T, Res>).GetField(nameof(result),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
            var resultFieldType = resultField.FieldType;
            bool isValueResult = typeof(Res).IsValueType;

            var target = GetTargetMethod();
            bool isStaticTarget = target != null && target.IsStatic;

            var paramTypes = isStaticTarget
                ? new[] { typeof(object[]), resultFieldType.MakeByRefType() }
                : new[] { typeof(T), typeof(object[]), resultFieldType.MakeByRefType() };

            int instanceIdx = 0;  // only used when !isStaticTarget
            int argsIdx = isStaticTarget ? 0 : 1;
            int resultIdx = isStaticTarget ? 1 : 2;

            var dm = new DynamicMethod(
                $"__Iridium_Postfix_{id}",
                typeof(void),
                paramTypes,
                typeof(StdPatchMethod<T, Res>).Module,
                true);

            var gen = dm.GetILGenerator();

            // var patch = (StdPatchMethod<T, Res>)_methods[id];

            if (!isStaticTarget)
            {
                // patch.instance = __instance
                EmitLoadInstance(gen);
                gen.Emit(OpCodes.Dup);
                gen.Emit(OpCodes.Ldarg, instanceIdx);
                gen.Emit(OpCodes.Stfld,
                    typeof(StdPatchMethod<T, Res>).GetField(nameof(instance),
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!);
            }
            else
            {
                EmitLoadInstance(gen);
            }

            // patch.result = __result   ← 读取原始返回值，让 Method 可以读取/修改
            gen.Emit(OpCodes.Ldarg, resultIdx);
            if (isValueResult)
                gen.Emit(OpCodes.Ldobj, resultFieldType);
            else
                gen.Emit(OpCodes.Ldind_Ref);
            gen.Emit(OpCodes.Stfld, resultField);

            // patch.Method(__args);  pop (ignore bool result)
            EmitLoadInstance(gen);
            gen.Emit(OpCodes.Ldarg, argsIdx);
            gen.Emit(OpCodes.Callvirt,
                typeof(StdPatchMethod<T, Res>).GetMethod(nameof(Method),
                    BindingFlags.Instance | BindingFlags.Public)!);
            gen.Emit(OpCodes.Pop);

            // __result = patch.result  ← 写回修改后的结果
            EmitLoadInstance(gen);
            gen.Emit(OpCodes.Ldfld, resultField);
            gen.Emit(OpCodes.Ldarg, resultIdx);
            if (isValueResult)
                gen.Emit(OpCodes.Stobj, resultFieldType);
            else
                gen.Emit(OpCodes.Stind_Ref);

            gen.Emit(OpCodes.Ret);
            return dm;
        }

        /// <summary>
        /// 生成 Transpiler DynamicMethod<br/>
        /// Harmony 签名: IEnumerable&lt;CodeInstruction&gt; Transpiler(
        ///     IEnumerable&lt;CodeInstruction&gt;, ILGenerator, MethodBase)
        /// </summary>
        private DynamicMethod BuildTranspiler()
        {
            var dm = new DynamicMethod(
                $"__Iridium_Transpiler_{id}",
                typeof(IEnumerable<CodeInstruction>),
                new[] { typeof(IEnumerable<CodeInstruction>), typeof(ILGenerator), typeof(MethodBase) },
                typeof(StdPatchMethod<T, Res>).Module,
                true);

            var gen = dm.GetILGenerator();

            // return patch.IL(instructions, generator, originalMethod);
            EmitLoadInstance(gen);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Ldarg_2);
            gen.Emit(OpCodes.Callvirt,
                typeof(StdPatchMethod<T, Res>).GetMethod("IL",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                    null,
                    new[] { typeof(IEnumerable<CodeInstruction>), typeof(ILGenerator), typeof(MethodBase) },
                    null)!);
            gen.Emit(OpCodes.Ret);

            return dm;
        }

        /// <summary>
        /// 加载 _methods[id] 并强转为 StdPatchMethod&lt;T, Res&gt;
        /// </summary>
        private void EmitLoadInstance(ILGenerator gen)
        {
            gen.Emit(OpCodes.Ldsfld,
                typeof(BasePatchMethod).GetField(nameof(_methods),
                    BindingFlags.Static | BindingFlags.Public)!);
            gen.Emit(OpCodes.Ldc_I4, id);
            gen.Emit(OpCodes.Callvirt,
                typeof(List<BasePatchMethod>).GetMethod("get_Item")!);
            gen.Emit(OpCodes.Castclass, typeof(StdPatchMethod<T, Res>));
        }

        internal override void ForceReset()
        {
            base.ForceReset();
            _dynPrefix = null;
            _dynPostfix = null;
            _dynTranspiler = null;
        }
    }
}
