using HarmonyLib;
using Iridium.Config;
using UnityEngine;

namespace Iridium.Patches.Optimizer
{
	[IriPatch(Path = "optimizer/scene", Pre = typeof(OptimizerSettings), Condition = "enableOptimizer,optimizeTileUpdate")]
	[HarmonyPatch(typeof(scrFloor), nameof(scrFloor.Awake))]
	public static class TileUpdateOptimizationPatch
	{
		public static void Postfix(scrFloor __instance)
		{
			if (!Main.Settings.optimizer.optimizeTileUpdate) return;
			var mr = __instance.GetComponent<MeshRenderer>();
			if (mr != null)
			{
				mr.receiveShadows = false;
				mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
				mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
			}
		}
	}
}
