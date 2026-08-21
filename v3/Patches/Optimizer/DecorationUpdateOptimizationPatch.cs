using HarmonyLib;
using Iridium.Config;
using UnityEngine;

namespace Iridium.Patches.Optimizer
{
	[IriPatch(Path = "optimizer/decor", Pre = typeof(OptimizerSettings), Condition = "enableOptimizer,optimizeDecorationUpdate")]
	[HarmonyPatch(typeof(scrVisualDecoration), nameof(scrVisualDecoration.Awake))]
	public static class DecorationUpdateOptimizationPatch
	{
		public static void Postfix(scrVisualDecoration __instance)
		{
			if (!Main.Settings.optimizer.optimizeDecorationUpdate) return;
			var sr = __instance.spriteRenderer;
			if (sr != null)
			{
				sr.receiveShadows = false;
				sr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
				sr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
			}
		}
	}
}
