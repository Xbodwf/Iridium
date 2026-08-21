using ADOFAI;
using HarmonyLib;
using Iridium.Config;
using System.Collections.Generic;

namespace Iridium.Patches.Optimizer
{
	[IriPatch(Path = "optimizer/scene", Pre = typeof(OptimizerSettings), Condition = "enableOptimizer,skipEventIfPaused")]
	[HarmonyPatch(typeof(scnGame), nameof(scnGame.ApplyEventsToFloors), typeof(List<scrFloor>), typeof(LevelData), typeof(scrLevelMaker), typeof(List<LevelEvent>))]
	public static class ApplyEventsToFloorsOptimizationPatch
	{
		[HarmonyPrefix]
		public static bool Prefix()
		{
			if (Main.Settings.optimizer.skipEventIfPaused && scrController.instance.paused)
			{
				return false;
			}
			return true;
		}
	}
}
