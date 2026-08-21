using Iridium.Config;
using ADOFAI;
using HarmonyLib;
using Iridium;
using UnityEngine;

namespace Iridium.Patches.UI
{
	[HarmonyPatch(typeof(scrUIController), "Update")]
	[IriPatch(Path = "ui/autoplayText", Pre = typeof(UISettings), Condition = "moveAutoplayText")]
	public static class AutoplayTextPositionPatch
	{
		private static bool _isAutoplayModified = false;
		private static Vector3 _originalAutoplayPos;

		[HarmonyPrefix]
		public static void Prefix()
		{
			RefreshAutoplayTextPosition();
		}

		public static void RefreshAutoplayTextPosition()
		{
			if (scrUIController.instance?.txtDebug == null) return;

			if (!_isAutoplayModified)
			{
				_originalAutoplayPos = scrUIController.instance.txtDebug.transform.localPosition;
				_isAutoplayModified = true;
			}

			scrUIController.instance.txtDebug.transform.localPosition = new Vector3(Main.Settings.ui.autoplayTextX, Main.Settings.ui.autoplayTextY, 0f);
		}
	}
}
