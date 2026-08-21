using HarmonyLib;
using Iridium.Config;
using UnityEngine;

namespace Iridium.Patches.Optimizer
{
	[IriPatch(Path = "optimizer/decor", Pre = typeof(OptimizerSettings), Condition = "enableOptimizer,!dontResizeCollider")]
	[HarmonyPatch(typeof(scrVisualDecoration), nameof(scrVisualDecoration.GetDecorationWorldSize))]
	public static class WorldSizeScalingPatch
	{
		public static void Postfix(scrVisualDecoration __instance, ref Vector2 __result)
		{
			if (Main.Settings.optimizer.dontResizeCollider) return;
			var tex = __instance.spriteRenderer?.sprite?.texture;
			if (tex != null && OptimizerShared.TryGetDecorRatioForTexture(tex, out Vector3 ratio))
			{
				__result = Vector2.Scale(__result, ratio);
			}
		}
	}
}
