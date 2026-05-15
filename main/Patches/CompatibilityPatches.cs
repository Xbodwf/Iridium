using System;
using System.Collections.Generic;
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

        [HarmonyPatch(typeof(scnGame), "ApplyCoreEventsToFloors", typeof(List<scrFloor>), typeof(LevelData), typeof(scrLevelMaker), typeof(List<LevelEvent>), typeof(List<LevelEvent>[]))]
        public static class LegacyPauseFixPatch_Apply
        {
            public static void Prefix(List<scrFloor> floors, LevelData levelData, scrLevelMaker lm, List<LevelEvent> events, List<LevelEvent>[] floorEvents)
            {
                if (!LegacyPauseFixPatch_Play.isPlayingFromEditor) return;
                if (floorEvents == null) return;

                bool isCCW = false;
                foreach (var floor in floors)
                {
                    var floorEventList = floorEvents[floor.seqID];
                    for (int i = 0; i < floorEventList.Count; i++)
                    {
                        if (floorEventList[i].eventType == LevelEventType.Twirl)
                            isCCW = !isCCW;
                    }
                    floor.isCCW = isCCW;
                }
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

                ADOBase.controller.mistakesManager.AddHit(HitMargin.FailOverload);
                ADOBase.controller.errorMeter?.AddHit(float.NegativeInfinity);
                ADOBase.controller.chosenPlanet.MarkFail()?.BlinkForSeconds(3);
            }

            public static void Postfix(scrDecoration __instance, HitboxType __state)
            {
                __instance.hitbox = __state;
            }
        }
    }
}
