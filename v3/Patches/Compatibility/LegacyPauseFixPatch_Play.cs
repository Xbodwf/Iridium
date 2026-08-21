using System;
using HarmonyLib;
using ADOFAI;
using Iridium.Config;

namespace Iridium.Patches.Compatibility
{
	[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.Play))]
	[IriPatch(Path = "compatibility/legacyPause", Pre = typeof(CompatibilitySettings), Condition = "enableLegacyPauseFix")]
	public static class LegacyPauseFixPatch_Play
	{
		public static bool isPlayingFromEditor = false;
		public static void Prefix()
		{
			isPlayingFromEditor = true;
		}
		public static Exception Finalizer(Exception __exception)
		{
			isPlayingFromEditor = false;
			return __exception;
		}
	}
}
