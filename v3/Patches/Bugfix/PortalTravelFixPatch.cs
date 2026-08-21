using Iridium.Config;
using System.Reflection;
using HarmonyLib;
using ADOFAI;

namespace Iridium.Patches.Bugfix
{
	[HarmonyPatch(typeof(scrController), nameof(scrController.PortalTravelAction))]
	[IriPatch(Path = "bugfix/portal", Pre = typeof(CompatibilitySettings), Condition = "portalTravelFix")]
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
}
