using Iridium.Config;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ADOFAI;
using HarmonyLib;
using UnityEngine;

namespace Iridium.Patches.UI
{
	[HarmonyPatch(typeof(FloorMesh), "SmallestAngleBetweenTwoAngles")]
	[IriPatch(Path = "ui/circleArc", Pre = typeof(UISettings), Condition = "enableCircleArc")]
	public static class CircleArcPatch
	{
		private static readonly MethodInfo ApplyOverride = AccessTools.Method(typeof(CircleArcPatch), nameof(ApplyCircleArcOverride));

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var codes = instructions.ToList();
			var resultLocal = generator.DeclareLocal(typeof(float));
			for (int i = 0; i < codes.Count; i++)
			{
				if (codes[i].opcode != OpCodes.Ret) continue;

				codes.Insert(i, new CodeInstruction(OpCodes.Stloc, resultLocal));
				codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldloc, resultLocal));
				codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldarg_1));
				codes.Insert(i + 3, new CodeInstruction(OpCodes.Ldarg_2));
				codes.Insert(i + 4, new CodeInstruction(OpCodes.Call, ApplyOverride));
				break;
			}
			return codes;
		}

		private static float ApplyCircleArcOverride(float original, float angleA, float angleB)
		{
			float minDiff = Mathf.Abs(Mathf.DeltaAngle(angleA * Mathf.Rad2Deg, angleB * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
			float minDiffDeg = minDiff * Mathf.Rad2Deg;
			if (minDiffDeg >= 89.9f && minDiffDeg <= 105.1f)
				return minDiff * 5f / 180f * Mathf.PI;
			return original;
		}
	}
}
