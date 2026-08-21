using System;
using System.Collections.Generic;
using HarmonyLib;
using ADOFAI;

namespace Iridium.Patches
{
	/// <summary>
	/// Third-party required mods handling ("Ignore required third-party mods").
	///
	/// Instead of patching the dependency check result, the chart's
	/// settings.requiredMods is emptied to [] right before the game decodes the
	/// level. Every piece of game logic that reads requiredMods (LevelDataCLS.Decode,
	/// LevelData.Decode, and any UI that displays a "mods required" notice) then
	/// sees an empty list, so the chart loads and plays like a normal level.
	///
	/// The original list is kept per LevelData instance and restored when the
	/// chart is encoded (saved/exported), so requiredMods survives a save.
	///
	/// Patches here are registered with the ignoreRequiredMods condition so the
	/// whole feature is applied/removed on demand.
	/// </summary>
	public static class RequiredModsClearPatches
	{
		private static readonly Dictionary<LevelData, object> OriginalRequiredMods = new();
		private static readonly List<string> CurrentRequiredMods = new();

		private static object ClearRequiredMods(Dictionary<string, object> settings)
		{
			if (settings == null || !settings.TryGetValue("requiredMods", out var raw)) return null;

			CurrentRequiredMods.Clear();
			if (raw is object[] arr)
			{
				foreach (var item in arr)
					if (item is string s && !string.IsNullOrEmpty(s)) CurrentRequiredMods.Add(s);
			}
			else if (raw is List<object> list)
			{
				foreach (var item in list)
					if (item is string s && !string.IsNullOrEmpty(s)) CurrentRequiredMods.Add(s);
			}

			settings["requiredMods"] = new List<object>();
			return raw;
		}

		/// <summary>
		/// Empty requiredMods in the editor/play decode path (LevelData.Decode)
		/// and stash the original value for the encode restore.
		/// </summary>
		[HarmonyPatch(typeof(LevelData), nameof(LevelData.Decode))]
		public static class LevelDataClearPatch
		{
			[HarmonyPrefix]
			public static void Prefix(LevelData __instance, Dictionary<string, object> dict)
			{
				if (!Main.Settings.compatibility.ignoreRequiredMods) return;
				if (dict != null && dict.TryGetValue("settings", out var s) && s is Dictionary<string, object> settings)
					OriginalRequiredMods[__instance] = ClearRequiredMods(settings);
			}
		}

		/// <summary>
		/// Empty requiredMods in the level select decode path (LevelDataCLS.Decode).
		/// </summary>
		[HarmonyPatch(typeof(LevelDataCLS), nameof(LevelDataCLS.Decode))]
		public static class LevelDataCLSClearPatch
		{
			[HarmonyPrefix]
			public static void Prefix(Dictionary<string, object> rootDict)
			{
				if (!Main.Settings.compatibility.ignoreRequiredMods) return;
				if (rootDict != null && rootDict.TryGetValue("settings", out var s) && s is Dictionary<string, object> settings)
					ClearRequiredMods(settings);
			}
		}

		/// <summary>
		/// Restore the original requiredMods before the chart is encoded so it is
		/// preserved when the level is saved or exported.
		/// </summary>
		[HarmonyPatch(typeof(LevelData), nameof(LevelData.EncodeToDictionary))]
		public static class EncodeRestorePatch
		{
			[HarmonyPrefix]
			public static void Prefix(LevelData __instance)
			{
				if (!Main.Settings.compatibility.ignoreRequiredMods) return;
				if (__instance == null) return;
				if (!OriginalRequiredMods.TryGetValue(__instance, out var raw) || raw == null) return;

				__instance.levelSettings["requiredMods"] = raw;
				OriginalRequiredMods.Remove(__instance);
			}
		}

		/// <summary>
		/// After a chart has finished loading (editor open or play), notify the
		/// player of the ignored missing mods.
		/// </summary>
		[HarmonyPatch(typeof(scnGame), nameof(scnGame.LoadLevel))]
		public static class LevelLoadNotifyPatch
		{
			[HarmonyPostfix]
			public static void Postfix(LoadResult status)
			{
				try
				{
					if (!Main.Settings.compatibility.ignoreRequiredMods) return;
					if (status != LoadResult.Successful) return;
					if (CurrentRequiredMods.Count == 0) return;

					var mods = CurrentRequiredMods.ToArray();
					CurrentRequiredMods.Clear();

					if (mods.Length == 1)
						UI.VRAMNotificationUI.Show(Localization.Get("MissingModsNoticeSingle", mods[0]));
					else
						UI.VRAMNotificationUI.Show(Localization.Get("MissingModsNotice", mods[0], mods.Length - 1));
				}
				catch (Exception e)
				{
					Main.Logger?.Log($"[RequiredMods] notify error: {e}");
				}
			}
		}
	}
}
