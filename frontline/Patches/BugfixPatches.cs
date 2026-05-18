using System.Reflection;
using HarmonyLib;
using ADOFAI;

namespace Iridium.Patches
{
    public static class BugfixPatches
    {
        [HarmonyPatch(typeof(scrController), "PortalTravelAction")]
        public static class PortalTravelFixPatch
        {
            private static FieldInfo? _f_transitioningLevel;
            private static System.Reflection.PropertyInfo? _p_isWipingToBlack;

            [HarmonyPrefix]
            public static bool Prefix(scrController __instance, Portal destination)
            {
                if (!Main.Settings.compatibility.portalTravelFix) return true;

                _f_transitioningLevel ??= AccessTools.Field(typeof(scrController), "transitioningLevel");

                if ((bool)_f_transitioningLevel.GetValue(__instance))
                    return false;

                var loader = ADOBase.loader;
                if (loader != null)
                {
                    _p_isWipingToBlack ??= AccessTools.Property(typeof(scrLoader), "isWipingToBlack");
                    if (_p_isWipingToBlack != null && (bool)_p_isWipingToBlack.GetValue(loader))
                        return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(OptionsPanelsCLS), "Awake")]
        public static class SyncSpeedTrialPatch
        {
            [HarmonyPostfix]
            public static void Postfix(OptionsPanelsCLS __instance)
            {
                if (!Main.Settings.compatibility.syncSpeedTrialOnLoad) return;

                if (GCS.speedTrialMode)
                {
                    __instance.speedTrial = true;
                    __instance.bgSprite.color = __instance.bgColorSpeedTrial;
                }
            }
        }
    }
}
