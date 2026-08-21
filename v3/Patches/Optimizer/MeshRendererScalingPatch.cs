using HarmonyLib;
using Iridium.Config;
using UnityEngine;

namespace Iridium.Patches.Optimizer
{
	[IriPatch(Path = "optimizer/decor", Pre = typeof(OptimizerSettings), Condition = "enableOptimizer")]
	[HarmonyPatch(typeof(scrVisualDecoration), nameof(scrVisualDecoration.UpdateShader))]
	public static class MeshRendererScalingPatch
	{
		public static void Postfix(scrVisualDecoration __instance)
		{
			if (GCS.internalLevelName != null) return;
			if (!__instance.meshRendererObj.activeSelf) return;
			var tex = __instance.spriteRenderer?.sprite?.texture;
			if (tex != null && OptimizerShared.TryGetDecorRatioForTexture(tex, out Vector3 ratio))
			{
				var localScale = __instance.meshRenderer.transform.localScale;
				__instance.meshRenderer.transform.localScale = new Vector3(
					localScale.x * ratio.x,
					localScale.y * ratio.y,
					localScale.z);
			}
		}
	}
}
