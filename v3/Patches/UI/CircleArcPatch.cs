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
			// num6 drives both the arc center (lerped from the corner intersection
			// toward the tile origin) and the radius (lerped 0..width): only large
			// num6 (~0.9) inflates the corner arc into the big rounded OUTER
			// corner. Vanilla keeps that look exclusively in the 89.9-105.1 band.
			// The application band is user-configurable (default 90-105, matching
			// vanilla); 180 must stay excluded so piAngle tiles keep their solid
			// fill and near-straight turns don't degenerate.
			var ui = Main.Settings?.ui;
			float min = Mathf.Clamp(ui != null ? ui.circleArcMinAngle : 90f, 0f, 180f);
			float max = Mathf.Clamp(ui != null ? ui.circleArcMaxAngle : 105f, min, 180f);
			if (minDiffDeg >= min && minDiffDeg <= max)
				return minDiff * 5f / 180f * Mathf.PI;
			return original;
		}
	}

	// All-angle arc corners. Vanilla FloorMesh.GetPositions draws the corner
	// arc only while angleDifference < 120 degrees; beyond that the corner
	// renders as a sharp point. GetPositions normalizes its inputs up front
	// (swapping angles whenever the directed difference exceeds 180 degrees),
	// which makes angleDifference always <= PI and turns the CCW gate into
	// dead code — so widening the single CW gate to PI lets CircleArcPatch's
	// inflated num6 (see ApplyCircleArcOverride) reach obtuse turns too.
	//
	// This patch intentionally does NOT touch num6: flooring it only produces
	// a tiny invisible inner fillet, and any num6 > 0 also shrinks the inner
	// inset (insetDistance0), which hollows out straight (piAngle) tiles.
	//
	// Shares the ui/circleArc condition with CircleArcPatch: one feature, one
	// switch.
	[HarmonyPatch(typeof(FloorMesh), "GetPositions")]
	[IriPatch(Path = "ui/circleArc", Pre = typeof(UISettings), Condition = "enableCircleArc", RequireMethod = "FloorMesh.GetPositions")]
	public static class AllAngleArcCornersPatch
	{
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var codes = instructions.ToList();

			// Verified against Assembly-CSharp 3.3.0 IL:
			//
			//   Anchor A — "angleDifference = ModAngle360(angle1 - angle0)":
			//     ldarg.0, ldarg.0, ldarg.2, ldarg.1, sub,
			//     call ModAngle360, stfld angleDifference
			//   angleDifference is a FIELD; the earlier ModAngle360 calls store
			//   back into arguments (starg), so call+stfld is unique.
			//
			//   Anchor C — the corner-arc gate:
			//     ldarg.0, ldfld angleDifference, ldc.r4 2.0942953, bge.un.s <skip arc>
			//   bge.un jumps PAST the arc body when angleDifference >= 120deg
			//   (or unordered), so replacing the constant with PI draws the arc
			//   for every turn below 180 degrees.

			// Anchor A.
			int anchorIdx = -1;
			for (int i = 0; i < codes.Count - 1; i++)
			{
				if (!IsCallTo(codes[i], "ModAngle360")) continue;
				if (codes[i + 1].opcode == OpCodes.Stfld &&
					codes[i + 1].operand is FieldInfo stored && stored.Name == "angleDifference")
				{
					anchorIdx = i;
					break;
				}
			}

			// Anchor C: first "ldfld angleDifference; ldc.r4 <not PI>" after the
			// anchor — that constant is the 120 degree gate threshold. (The
			// later "angleDifference < PI" ternary is excluded by the PI check.)
			int gateIdx = -1;
			for (int i = anchorIdx + 2; anchorIdx >= 0 && i < codes.Count - 2; i++)
			{
				if (codes[i].opcode != OpCodes.Ldfld ||
					codes[i].operand is not FieldInfo loaded || loaded.Name != "angleDifference")
					continue;
				if (codes[i + 1].opcode != OpCodes.Ldc_R4 || codes[i + 1].operand is not float threshold)
					continue;
				if (Mathf.Approximately(threshold, Mathf.PI)) continue;
				gateIdx = i + 1;
				break;
			}

			if (anchorIdx < 0 || gateIdx < 0)
			{
				return instructions;
			}

			var result = new List<CodeInstruction>(codes.Count);
			for (int i = 0; i < codes.Count; i++)
			{
				if (i == gateIdx)
				{
					var widened = new CodeInstruction(codes[i]) { operand = Mathf.PI };
					result.Add(widened);
				}
				else
				{
					result.Add(codes[i]);
				}
			}
			return result;
		}

		// Match by name: Harmony resolves instruction operands through the
		// runtime module, so ReferenceEquals against an AccessTools-resolved
		// MethodInfo is not reliable.
		private static bool IsCallTo(CodeInstruction instruction, string methodName)
		{
			if (instruction.opcode != OpCodes.Call && instruction.opcode != OpCodes.Callvirt) return false;
			return instruction.operand is MethodInfo m &&
				   m.DeclaringType == typeof(FloorMesh) &&
				   m.Name == methodName;
		}
	}
}
