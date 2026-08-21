using Iridium.Config;
using ADOFAI;
using HarmonyLib;

namespace Iridium.Patches.UI
{
	[HarmonyPatch(typeof(scnGame), nameof(scnGame.Play))]
	[IriPatch(Path = "ui/countdown", Pre = typeof(UISettings), Condition = "alwaysCountdown")]
	public static class AlwaysCountdownPatch
	{
		private static bool _tempAuto;

		public static void Prefix()
		{
			if (!Main.Settings.ui.alwaysCountdown || !ADOBase.isLevelEditor) return;
			_tempAuto = RDC.auto;
			RDC.auto = false;
		}

		public static void Postfix()
		{
			if (!Main.Settings.ui.alwaysCountdown || !ADOBase.isLevelEditor) return;
			RDC.auto = _tempAuto;
		}
	}
}
