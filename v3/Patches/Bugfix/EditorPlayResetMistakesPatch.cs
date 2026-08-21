using Iridium.Config;
using HarmonyLib;
using ADOFAI;

namespace Iridium.Patches.Bugfix
{
	[HarmonyPatch(typeof(scnGame), nameof(scnGame.Play))]
	[IriPatch(Path = "bugfix/editorPlayReset", Pre = typeof(CompatibilitySettings), Condition = "fixEditorPlayResetMistakes")]
	public static class EditorPlayResetMistakesPatch
	{
		[HarmonyPrefix]
		public static void Prefix()
		{
			if (!ADOBase.isLevelEditor) return;
			scrController.instance?.mistakesManager.Reset();
		}
	}
}
