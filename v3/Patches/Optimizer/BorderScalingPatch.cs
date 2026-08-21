using ADOFAI;
using HarmonyLib;
using Iridium.Config;
using System.Collections.Generic;
using UnityEngine;

namespace Iridium.Patches.Optimizer
{
	[IriPatch(Path = "optimizer/decor", Pre = typeof(OptimizerSettings), Condition = "enableOptimizer")]
	[HarmonyPatch(typeof(scrDecorationManager), "UpdateBordersSizes")]
	public static class BorderScalingPatch
	{
		public static void Postfix(scrDecorationManager __instance)
		{
			if (Main.Settings.optimizer.dontResizeCollider) return;

			var selected = ADOBase.editor?.selectedDecorations;
			if (selected == null || selected.Count == 0) return;

			var targets = new List<scrVisualDecoration>(selected.Count + 1);
			foreach (var ev in selected)
			{
				if (ev != null && scrDecorationManager.GetDecoration(ev) is scrVisualDecoration decor)
					targets.Add(decor);
			}

			var hoveredDecor = __instance.hoveredDecoration != null
				? scrDecorationManager.GetDecoration(__instance.hoveredDecoration) as scrVisualDecoration
				: null;

			if (hoveredDecor != null)
			{
				if (ADOBase.editor != null && ADOBase.editor.decorations.Contains(__instance.hoveredDecoration))
				{
					if (!targets.Contains(hoveredDecor))
						targets.Add(hoveredDecor);
				}
				else
				{
					targets.Remove(hoveredDecor);
				}
			}

			foreach (var decor in targets)
			{
				if (decor.spriteRenderer?.sprite == null) continue;
				float ppu = decor.spriteRenderer.sprite.pixelsPerUnit;
				float offset = 0.5f / ppu;
				Vector3 baseScale = decor.transform.localScale;
				Vector2 sign = new(offset * Mathf.Sign(baseScale.x), offset * Mathf.Sign(baseScale.y));
				Vector3 ratio = decor.spriteRenderer.transform.localScale;
				decor.bordersRenderer.size = Vector2.Scale(decor.bordersRenderer.size - sign, ratio) + sign;
				decor.cachedBorderSize = decor.bordersRenderer.size;
			}
		}
	}
}
