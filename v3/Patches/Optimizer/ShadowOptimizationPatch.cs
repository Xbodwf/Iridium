using HarmonyLib;
using Iridium.Config;
using UnityEngine;

namespace Iridium.Patches.Optimizer
{
	[IriPatch(Path = "optimizer", Pre = typeof(OptimizerSettings), Condition = "enableOptimizer,!disableShadows")]
	[HarmonyPatch(typeof(scnGame), nameof(scnGame.LoadLevel))]
	public static class ShadowOptimizationPatch
	{
		public static void Postfix()
		{
			if (Main.Settings.optimizer.disableShadows)
			{
				QualitySettings.shadows = ShadowQuality.Disable;
			}
		}
	}
}
