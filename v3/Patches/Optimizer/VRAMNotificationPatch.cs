using HarmonyLib;
using Iridium.Config;
using Iridium.UI;

namespace Iridium.Patches.Optimizer
{
	[IriPatch(Path = "optimizer", Pre = typeof(OptimizerSettings), Condition = "enableOptimizer")]
	[HarmonyPatch(typeof(scnGame), nameof(scnGame.UpdateDecorationObjects))]
	public static class VRAMNotificationPatch
	{
		public static bool isFinished = false;
		public static void Postfix(bool reloadDecorations)
		{
			if (isFinished || !reloadDecorations || Main.Settings.optimizer.dontShowSavedMemory) return;

			if (OptimizerShared.savedVRAM_MB > 0.1f)
			{
				Iridium.UI.VRAMNotificationUI.Show(Localization.Get("SavedMemoryMsg", OptimizerShared.savedVRAM_MB.ToString("F2")));
				Main.Logger?.Log(Localization.Get("SavedMemoryLog", OptimizerShared.savedVRAM_MB.ToString("F2")));
			}
			isFinished = true;
		}
	}
}
