using Iridium.Config;
using System;
using HarmonyLib;

namespace Iridium.Patches.Bugfix
{
	[HarmonyPatch(typeof(scrLevelMaker), nameof(scrLevelMaker.CalculateSingleFloorAngleLength))]
	[IriPatch(Path = "bugfix/turnaround", Pre = typeof(CompatibilitySettings), Condition = "fixTurnaroundCondition")]
	public static class TurnaroundConditionFix
	{
		private const double TwoPi = 6.2831854820251465;
		private const double TurnaroundEpsilon = 0.0001;

		[HarmonyPostfix]
		public static void Postfix(scrFloor cf)
		{
			if (cf.turnaround)
			{
				double angleMoved = scrMisc.GetAngleMoved(cf.entryangle, cf.exitangle, !cf.isCCW);
				if (Math.Abs(angleMoved - TwoPi) >= TurnaroundEpsilon)
					cf.turnaround = false;
			}
		}
	}
}
