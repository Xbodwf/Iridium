using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Iridium.Core;
using UnityEngine;

namespace Iridium.Patches
{
    internal static class RTMaxSizePatch
    {
        internal static bool IsEnabled =>
            Main.Settings?.optimizer is { enableOptimizer: true, optimizeDecorationAnimation: true };

        // ===== Clamp helpers =====
        private static (int w, int h) ClampedSize(int texW, int texH)
        {
            int maxDim = Mathf.Max(Screen.width, Screen.height);
            float ratio = Mathf.Min((float)maxDim / texW, (float)maxDim / texH);
            if (ratio >= 1f) return (texW, texH);
            return (Mathf.Max(1, Mathf.RoundToInt(texW * ratio)),
                    Mathf.Max(1, Mathf.RoundToInt(texH * ratio)));
        }

        // For replacing Texture::get_width in the size check → so clamped RT always matches
        internal static int ClampedTextureWidth(Texture t)
        {
            if (!IsEnabled) return t.width;
            var (w, _) = ClampedSize(t.width, t.height);
            return w;
        }

        internal static int ClampedTextureHeight(Texture t)
        {
            if (!IsEnabled) return t.height;
            var (_, h) = ClampedSize(t.width, t.height);
            return h;
        }

        internal static RenderTexture ClampedGetEmptyRT(Texture texture, int depth, RenderTextureFormat format)
        {
            if (!IsEnabled)
                return new RenderTexture(texture.width, texture.height, depth, format);
            var (w, h) = ClampedSize(texture.width, texture.height);
            Main.Logger?.Log($"[RTMaxSize] Create main RT: {w}x{h} (orig {texture.width}x{texture.height})");
            return new RenderTexture(w, h, depth, format);
        }

        private static bool _tempRtLogged;

        internal static RenderTexture ClampedGetTempRT(Texture texture, int depth, RenderTextureFormat format)
        {
            if (!IsEnabled)
                return RenderTexture.GetTemporary(texture.width, texture.height, depth, format);
            var (w, h) = ClampedSize(texture.width, texture.height);
            if (!_tempRtLogged) { _tempRtLogged = true; Main.Logger?.Log($"[RTMaxSize] Create temp RT: {w}x{h} (orig {texture.width}x{texture.height})"); }
            return RenderTexture.GetTemporary(w, h, depth, format);
        }


        // ===== Patch 1: UpdateShader =====
        [HarmonyPatch(typeof(scrVisualDecoration), "UpdateShader")]
        internal static class UpdateShaderPatch
        {
            static UpdateShaderPatch()
            {
                Main.Logger?.Log("[RTMaxSize] UpdateShaderPatch loaded");
            }

            private static bool _prefixHit;
            private static bool _diagLogged;
            private static bool _replacedGetWidth;
            private static bool _replacedGetHeight;
            private static bool _replacedGetTempRT;
            private static int _prefixCallCount;

            [HarmonyPrefix]
            static void Prefix(scrVisualDecoration __instance)
            {
                _prefixCallCount++;
                if (_prefixCallCount <= 3 && Main.Logger != null)
                    Main.Logger.Log($"[RTMaxSize] Prefix call #{_prefixCallCount} on {__instance?.name}");

                if (!_prefixHit) { _prefixHit = true; Main.Logger?.Log("[RTMaxSize] Prefix entered"); }

                var sprite = __instance?.spriteRenderer.sprite;
                if (sprite == null) return;
                var tex = sprite.texture;
                if (tex == null) return;
                var mat = __instance?.meshRenderer.material;
                if (mat == null) return;
                var rt = mat.mainTexture as RenderTexture;
                if (rt == null) return;
                int expectedW, expectedH;
                if (IsEnabled)
                    (expectedW, expectedH) = ClampedSize(tex.width, tex.height);
                else
                    (expectedW, expectedH) = (tex.width, tex.height);
                if (rt.width == expectedW && rt.height == expectedH)
                {
                    if (!_diagLogged) { _diagLogged = true; Main.Logger?.Log($"[RTMaxSize] Match: rt={rt.width}x{rt.height} expected={expectedW}x{expectedH} tex={tex.width}x{tex.height} scrn={Screen.width}x{Screen.height}"); }
                    return;
                }
                Main.Logger?.Log($"[RTMaxSize] {__instance?.name}: RT resized {rt.width}x{rt.height} → {expectedW}x{expectedH}");
                mat.mainTexture = null;
                rt.Release();
            }
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var clampedWidth = AccessTools.Method(typeof(RTMaxSizePatch), nameof(ClampedTextureWidth), new[] { typeof(Texture) });
                var clampedHeight = AccessTools.Method(typeof(RTMaxSizePatch), nameof(ClampedTextureHeight), new[] { typeof(Texture) });
                var clampedGetTempRT = AccessTools.Method(typeof(RTMaxSizePatch), nameof(ClampedGetTempRT), new[] { typeof(Texture), typeof(int), typeof(RenderTextureFormat) });

                foreach (var instr in instructions)
                {
                    if ((instr.opcode == OpCodes.Call || instr.opcode == OpCodes.Callvirt) && instr.operand is MethodInfo mi)
                    {
                        if (mi.DeclaringType == typeof(RDExtensions) && mi.Name == nameof(RDExtensions.GetTempRT))
                        {
                            if (clampedGetTempRT != null)
                            {
                                if (!_replacedGetTempRT) { _replacedGetTempRT = true; Main.Logger?.Log("[RTMaxSize] Transpiler replaced GetTempRT"); }
                                yield return new CodeInstruction(OpCodes.Call, clampedGetTempRT);
                            }
                            else
                                yield return instr;
                            continue;
                        }
                        if (mi.Name == "get_width" && mi.DeclaringType == typeof(Texture) && clampedWidth != null)
                        {
                            if (!_replacedGetWidth) { _replacedGetWidth = true; Main.Logger?.Log("[RTMaxSize] Transpiler replaced get_width"); }
                            yield return new CodeInstruction(OpCodes.Call, clampedWidth);
                            continue;
                        }
                        if (mi.Name == "get_height" && mi.DeclaringType == typeof(Texture) && clampedHeight != null)
                        {
                            if (!_replacedGetHeight) { _replacedGetHeight = true; Main.Logger?.Log("[RTMaxSize] Transpiler replaced get_height"); }
                            yield return new CodeInstruction(OpCodes.Call, clampedHeight);
                            continue;
                        }
                    }
                    yield return instr;
                }
            }

            [HarmonyPostfix]
            static void Postfix(scrVisualDecoration __instance)
            {
                // Revert localScale back to original texture size
                // (Transpiler replaces ALL Texture::get_width, including localScale calc)
                var sprite = __instance.spriteRenderer.sprite;
                if (sprite == null) return;
                var tex = sprite.texture;
                if (tex == null) return;
                __instance.meshRenderer.transform.localScale = new Vector3(
                    tex.width / 100f, tex.height / 100f, 1f);
            }
        }

        // ===== Patch 2: EnableShader local function =====
        [HarmonyPatch]
        internal static class EnableShaderPatch
        {
            static EnableShaderPatch()
            {
                Main.Logger?.Log("[RTMaxSize] EnableShaderPatch loaded");
            }

            [HarmonyTargetMethod]
            static MethodBase? TargetMethod()
            {
                foreach (var m in typeof(scrVisualDecoration).GetMethods(BindingFlags.NonPublic | BindingFlags.Static))
                {
                    if (m.Name.Contains("g__EnableShader") &&
                        m.GetCustomAttribute<CompilerGeneratedAttribute>() != null)
                        return m;
                }
                return null;
            }

            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return ReplaceCalls(instructions, nameof(RDExtensions.GetEmptyRT));
            }
        }

        // ===== Common IL replacement =====
        private static IEnumerable<CodeInstruction> ReplaceCalls(
            IEnumerable<CodeInstruction> instructions,
            string originalMethodName)
        {
            var replacement = AccessTools.Method(typeof(RTMaxSizePatch), nameof(ClampedGetEmptyRT), new[] { typeof(Texture), typeof(int), typeof(RenderTextureFormat) });
            foreach (var instr in instructions)
            {
                if (instr.opcode == OpCodes.Call &&
                    instr.operand is MethodInfo mi &&
                    mi.Name == originalMethodName &&
                    mi.DeclaringType == typeof(RDExtensions) &&
                    replacement != null)
                {
                    yield return new CodeInstruction(OpCodes.Call, replacement);
                }
                else
                {
                    yield return instr;
                }
            }
        }
    }
}
