using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using DG.Tweening;

namespace Iridium.Patches
{
	public static class FfxOptimizationPatches
	{
		private static bool ShouldUpdate(scrDecoration dec)
		{
			if (dec == null || !dec.GetVisible()) return false;

			if (dec.eventTweens != null && dec.eventTweens.Count > 0)
			{
				foreach (var tween in dec.eventTweens.Values)
				{
					if (tween != null && tween.IsActive() && tween.IsPlaying())
					{
						return true;
					}
				}
			}

			if (dec.parallax != null && (dec.parallax.multiplier.x != 1f || dec.parallax.multiplier.y != 1f))
			{
				return true;
			}

			return false;
		}

		[HarmonyPatch(typeof(scrDecorationManager), "LateUpdate")]
		public static class OptimizeDecorationManagerLateUpdate
		{
			static bool Prefix(scrDecorationManager __instance)
			{
				if (!Main.Settings.optimizer.enableOptimizer || !Main.Settings.optimizer.optimizeFfxDecorations)
					return true;

				try
				{
					var allDecorations = __instance.allDecorations;
					int count = allDecorations.Count;

					for (int i = 0; i < count; i++)
					{
						scrDecoration dec = allDecorations[i];
						if (ShouldUpdate(dec))
						{
							dec.UpdatePosition();
						}
					}

					return false;
				}
				catch (Exception ex)
				{
					Main.Logger?.Error($"[FfxOptimization] Error in OptimizeDecorationManagerLateUpdate: {ex}");
					return true;
				}
			}
		}
	}
}
