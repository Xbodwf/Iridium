using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using ADOFAI;
using HarmonyLib;

namespace Iridium.Patches
{
    public static class CompatibilityPatches
    {
        [HarmonyPatch(typeof(scnEditor), nameof(scnEditor.Play))]
        public static class LegacyPauseFixPatch_Play
        {
            public static bool isPlayingFromEditor = false;
            public static void Prefix()
            {
                isPlayingFromEditor = true;
            }
            public static Exception Finalizer(Exception __exception)
            {
                isPlayingFromEditor = false;
                return __exception;
            }
        }

        [HarmonyPatch(typeof(scrDecoration), "HitboxTriggerAction")]
        public static class NoFailTooEarlyPatch
        {
            public static void Prefix(scrDecoration __instance, out HitboxType __state, scrPlanet planet)
            {
                __state = __instance.hitbox;
                if (!ADOBase.controller.gameworld || !ADOBase.controller.noFail || __instance.hitbox != HitboxType.Kill)
                {
                    return;
                }

                if (RDC.auto)
                {
                    return;
                }

                __instance.hitbox = HitboxType.None;
                if ((planet != null && planet.iFrames > 0) || __instance.hitOnce)
                {
                    return;
                }

                ADOBase.controller.playerOne.marginTracker.AddHit(HitMargin.FailOverload);
                ADOBase.controller.errorMeter?.AddHit(float.NegativeInfinity);
                ADOBase.controller.chosenPlanet.MarkFail()?.BlinkForSeconds(3);
            }

            public static void Postfix(scrDecoration __instance, HitboxType __state)
            {
                __instance.hitbox = __state;
            }
        }

        /// <summary>
        /// 动态滤镜/特效速度随音高变化
        /// 替换 AdjustDurationForHardbake 中的 customLevel getter，
        /// 当设置开启时返回 null，使原方法 !null=true 自动执行 duration /= pitch
        /// </summary>
        [HarmonyPatch(typeof(ffxPlusBase), "AdjustDurationForHardbake")]
        public static class ScaleFilterSpeedWithPitchPatch
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var getter = AccessTools.PropertyGetter(typeof(ADOBase), "customLevel");
                var wrapper = AccessTools.Method(typeof(ScaleFilterSpeedWithPitchPatch), nameof(GetCustomLevel));

                foreach (var code in instructions)
                {
                    if (code.Calls(getter))
                        yield return new CodeInstruction(OpCodes.Call, wrapper);
                    else
                        yield return code;
                }
            }

            private static scnGame GetCustomLevel()
            {
                return Main.Settings.compatibility.scaleFilterSpeedWithPitch ? null : ADOBase.customLevel;
            }
        }
    }
}
