using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using GDMiniJSON;
using HarmonyLib;
using ADOFAI;
using Iridium.Config;

namespace Iridium.Patches.Optimizer
{
	[IriPatch(Path = "optimizer/json", Pre = typeof(OptimizerSettings), Condition = "customLevelReadOptimization")]
	[HarmonyPatch(typeof(LevelData), nameof(LevelData.GetCustomLevelName))]
	public static class PatchGetCustomLevelName
	{
		private static readonly MethodInfo _deserializePartially = typeof(Json).GetMethod(nameof(Json.DeserializePartially), new[] { typeof(string), typeof(string) });

		[HarmonyPrefix]
		public static bool Prefix(string path, ref string __result)
		{
			if (!Main.Settings.optimizer.customLevelReadOptimization)
				return true;

			try
			{
				string json = RDFile.ReadAllText(path);
				if (json == null)
				{
					__result = "";
					return false;
				}

				var root = Json.DeserializePartially(json, "actions") as Dictionary<string, object>;
				if (root == null || !root.TryGetValue("settings", out var settingsObj))
				{
					root = Json.Deserialize(json) as Dictionary<string, object>;
					if (root == null || !root.TryGetValue("settings", out settingsObj))
					{
						__result = "";
						return false;
					}
				}

				var settings = settingsObj as Dictionary<string, object>;
				if (settings == null)
				{
					__result = "";
					return false;
				}

				string? song = settings.TryGetValue("song", out var s) ? s as string : null;
				string? artist = settings.TryGetValue("artist", out var a) ? a as string : null;
				string result;

				if (string.IsNullOrEmpty(song))
				{
					result = "";
				}
				else if (string.IsNullOrEmpty(artist))
				{
					result = song!;
				}
				else
				{
					string artistTrimmed = artist!.Trim();
					if (artistTrimmed.EndsWith(")"))
					{
						int idx = artistTrimmed.IndexOf("(");
						if (idx > 0)
							artistTrimmed = artistTrimmed.Substring(0, idx).Trim();
					}
					result = artistTrimmed + " - " + song;
				}

				__result = RDUtils.RemoveRichTags(result);
				return false;
			}
			catch (Exception e)
			{
				Main.Logger?.Error($"[JsonPatches] GetCustomLevelName failed: {e.Message}");
				return true;
			}
		}
	}
}
