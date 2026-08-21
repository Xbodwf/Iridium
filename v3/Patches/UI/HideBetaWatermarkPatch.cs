using Iridium.Config;
using ADOFAI;
using HarmonyLib;
using Iridium;
using UnityEngine;

namespace Iridium.Patches.UI
{
	[HarmonyPatch(typeof(scrEnableIfBeta), "Awake")]
	[IriPatch(Path = "ui/watermark", Pre = typeof(UISettings), Condition = "hideBetaWatermark")]
	public static class HideBetaWatermarkPatch
	{
		public static void Postfix(scrEnableIfBeta __instance)
		{
			if (Main.Settings.ui.hideBetaWatermark)
				__instance.gameObject.SetActive(false);
		}

		public static void RefreshBetaWatermark()
		{
			var hide = Main.Settings.ui.hideBetaWatermark;
			foreach (var watermark in Resources.FindObjectsOfTypeAll<scrEnableIfBeta>())
			{
				watermark.gameObject.SetActive(!hide);
			}
		}
	}
}
