using ADOFAI;
using HarmonyLib;
using Iridium.Config;

namespace Iridium.Patches.Optimizer
{
	[IriPatch(Path = "optimizer/scene", Pre = typeof(OptimizerSettings), Condition = "enableOptimizer,optimizeEventIcons")]
	[HarmonyPatch(typeof(scrFloor), nameof(scrFloor.UpdateIconSprite))]
	public static class UpdateIconSpriteOptimizationPatch
	{
		[HarmonyPrefix]
		public static bool Prefix()
		{
			if (Main.Settings.optimizer.optimizeEventIcons && !ADOBase.isLevelEditor && !Main.IsMainThread)
			{
				return false;
			}
			return true;
		}
	}
}
