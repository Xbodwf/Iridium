using System.Collections.Generic;
using ADOFAI;
using HarmonyLib;
using Iridium;
using Iridium.Config;

namespace Iridium.Patches.UI
{
	[HarmonyPatch(typeof(scrConductor), "Update")]
	[IriPatch(Path = "ui/lobbyMusic", Pre = typeof(LobbyMusicSettings), Condition = "enableCustomBpm")]
	public static class CustomBpmPatch
	{
		private static readonly HashSet<string> LobbyScenes = new()
		{
			"scnLevelSelect",
			"scnCLS",
			"scnTaroMenu0",
			"scnTaroMenu1",
			"scnTaroMenu2",
			"scnTaroMenu3"
		};

		[HarmonyPrefix]
		public static void Prefix()
		{
			UpdateBpm();
		}

		public static void UpdateBpm()
		{
			if (!Main.Settings.lobbyMusic.enableCustomBpm || scrConductor.instance is null) return;
			if (!LobbyScenes.Contains(ADOBase.sceneName)) return;

			scrConductor.instance.bpm = Main.Settings.lobbyMusic.customBpm;
		}
	}
}
