using ADOFAI;
using HarmonyLib;
using Iridium.Config;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Iridium.Patches.Optimizer
{
	[IriPatch(Path = "optimizer", Pre = typeof(OptimizerSettings), Condition = "enableOptimizer")]
	[HarmonyPatch(typeof(scnGame))]
	public static class OptimizationResetPatches
	{
		[HarmonyPatch("OnDestroy")]
		[HarmonyPatch(nameof(scnGame.LoadAndPlayLevel))]
		[HarmonyPrefix]
		public static void FullReset()
		{
			OptimizerShared.ResetDecorOptimization(true);
		}

		[HarmonyPatch(nameof(scnGame.LoadLevel)), HarmonyPrefix]
		public static void SoftReset() => OptimizerShared.ResetDecorOptimization(false);

		[HarmonyPatch("Awake"), HarmonyPostfix]
		public static void CleanupTrackCache()
		{
			TrackOptimizationPatches._floorTransformCache = new ConditionalWeakTable<scrFloor, Transform>();
		}
	}
}
