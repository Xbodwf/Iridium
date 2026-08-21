using Iridium.Config;
using ADOFAI;
using HarmonyLib;

namespace Iridium.Patches.UI
{
	[HarmonyPatch(typeof(scrMisc), nameof(scrMisc.DetermineDifficultyUIMode))]
	[IriPatch(Path = "ui/difficulty", Pre = typeof(UISettings), Condition = "forceDifficultyUI")]
	public static class ForceDifficultyUIPatch
	{
		public static void Postfix(ref DifficultyUIMode __result)
		{
			if (ADOBase.isCLSLevel) __result = DifficultyUIMode.ShowAll;
		}
	}
}
