using HarmonyLib;
using Iridium.Config;
using UnityEngine;

namespace Iridium.Patches.Optimizer
{
	[IriPatch(Path = "optimizer/decor", Pre = typeof(OptimizerSettings), Condition = "enableOptimizer,!dontResizeCollider")]
	[HarmonyPatch(typeof(scrVisualDecoration), nameof(scrVisualDecoration.UpdateHitbox))]
	public static class VisualDecorationUpdateHitboxPatch
	{
		[HarmonyPrefix]
		public static bool Prefix(scrVisualDecoration __instance)
		{
			if (Main.Settings.optimizer.fastLoading && scnEditor.instance == null)
			{
				if (__instance.damageBox != null && __instance.damageBox.enabled)
					__instance.damageBox.enabled = false;
			}
			return true;
		}

		[HarmonyPostfix]
		public static void Postfix(scrVisualDecoration __instance)
		{
			if (!__instance.useHitbox || __instance.spriteRenderer == null) return;
			Vector3 ratio = __instance.spriteRenderer.transform.localScale;
			if (__instance.hitboxType == Hitbox.Box)
			{
				if (__instance.damageBox != null)
					__instance.damageBox.size = Vector2.Scale(__instance.damageBox.size, ratio);
			}
			else if (__instance.hitboxType == Hitbox.Capsule)
			{
				if (__instance.damageCapsule != null)
					__instance.damageCapsule.size = Vector2.Scale(__instance.damageCapsule.size, ratio);
			}
			else
			{
				if (__instance.damageCircle != null)
					__instance.damageCircle.radius *= ratio.x;
			}
		}
	}
}
