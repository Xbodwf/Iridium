using ADOFAI;
using HarmonyLib;
using Iridium.Config;
using UnityEngine;

namespace Iridium.Patches.Optimizer
{
	[IriPatch(Path = "optimizer/decor", Pre = typeof(OptimizerSettings), Condition = "enableOptimizer,!dontResizeCollider")]
	[HarmonyPatch(typeof(scrDecorationManager), "UpdateHitboxSizes")]
	public static class HitboxScalingPatch
	{
		public static void Postfix()
		{
			if (Main.Settings.optimizer.dontResizeCollider) return;

			var selected = ADOBase.editor?.selectedDecorations;
			if (selected == null || selected.Count == 0) return;

			foreach (var ev in selected)
			{
				if (ev == null) continue;
				var decor = scrDecorationManager.GetDecoration(ev) as scrVisualDecoration;
				if (decor?.spriteRenderer != null)
				{
					decor.hitboxRenderer.size = Vector2.Scale(decor.hitboxRenderer.size, decor.spriteRenderer.transform.localScale);
				}
			}
		}
	}
}
