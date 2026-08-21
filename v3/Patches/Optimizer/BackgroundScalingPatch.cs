using HarmonyLib;
using Iridium.Config;
using UnityEngine;

namespace Iridium.Patches.Optimizer
{
	[IriPatch(Path = "optimizer/decor", Pre = typeof(OptimizerSettings), Condition = "enableOptimizer")]
	[HarmonyPatch(typeof(scrCustomBackgroundSprite), nameof(scrCustomBackgroundSprite.SetCustomBG))]
	public static class BackgroundScalingPatch
	{
		public static void Postfix(scrCustomBackgroundSprite __instance)
		{
			var sprite = __instance.displayedSprite?.sprite;
			if (sprite?.texture == null) return;

			if (OptimizerShared.TryGetDecorRatioForTexture(sprite.texture, out Vector3 ratio))
			{
				__instance.imgSize = Vector2.Scale(__instance.imgSize, ratio);
			}
		}
	}
}
