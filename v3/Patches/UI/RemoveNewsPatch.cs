using Iridium.Config;
using ADOFAI;
using HarmonyLib;
using Iridium;
using UnityEngine;

namespace Iridium.Patches.UI
{
	[HarmonyPatch(typeof(scnLevelSelect))]
	[IriPatch(Path = "ui/news", Pre = typeof(UISettings), Condition = "removeNews")]
	public static class RemoveNewsPatch
	{
		internal static GameObject? newsContainer = null;

		[HarmonyPatch("Awake"), HarmonyPostfix]
		public static void Postfix()
		{
			newsContainer = GameObject.Find("News Container");
		}

		[HarmonyPatch("Update"), HarmonyPrefix]
		public static void Prefix()
		{
			UpdateNews();
		}

		public static void UpdateNews()
		{
			if (newsContainer is null) return;
			bool shouldBeActive = !Main.Settings.ui.removeNews;
			if (newsContainer.activeSelf != shouldBeActive) newsContainer.SetActive(shouldBeActive);
		}
	}
}
