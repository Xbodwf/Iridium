using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using GDMiniJSON;
using HarmonyLib;
using ADOFAI;
using Iridium.Config;

namespace Iridium.Patches
{
	public static class JsonPatches
	{
		private static readonly MethodInfo _deserializePartially = typeof(Json).GetMethod(nameof(Json.DeserializePartially), new[] { typeof(string), typeof(string) });

		private static float PathIdToRadiansSafe(char c)
		{
			var partial = AccessTools.Method(typeof(FloorHelper), nameof(FloorHelper.PathIdToRadiansPartial));
			if (partial != null)
			{
				double? result = (double?)partial.Invoke(null, new object[] { c });
				return result.HasValue ? (float)result.Value : 999f;
			}
			return 999f;
		}

		/// <summary>
		/// Safe Replace：Replace Json.Deserialize(str) with Json.DeserializePartially(str, "actions")。
		/// If _deserializePartially is null (method not found), skip the replacement and use the original Deserialize.
		/// </summary>
		private static IEnumerable<CodeInstruction> ReplaceDeserializeWithPartially(IEnumerable<CodeInstruction> instructions)
		{
			if (_deserializePartially == null)
			{
				Main.Logger?.Warning("[JsonPatches] DeserializePartially method not found, skipping optimization");
				return instructions;
			}

			var list = new List<CodeInstruction>();
			foreach (var inst in instructions)
			{
				if (inst.opcode == OpCodes.Call && inst.operand is MethodInfo method &&
					method.Name == nameof(Json.Deserialize) && method.DeclaringType == typeof(Json))
				{
					list.Add(new CodeInstruction(OpCodes.Ldstr, "actions"));
					list.Add(new CodeInstruction(OpCodes.Call, _deserializePartially));
				}
				else
				{
					list.Add(inst);
				}
			}
			return list;
		}

		/// <summary>
		/// LevelData.GetCustomLevelName only get level settings，so it's no need to parse actions array。
		/// use DeserializePartially(str, "actions") and stop when meet "actions" 。
		/// Safety fix：if settings not found（in JSON,actions appears earlier than settings），return "" to prevent crashes.
		/// </summary>
		[HarmonyPatch(typeof(LevelData), nameof(LevelData.GetCustomLevelName))]
		public static class PatchGetCustomLevelName
		{
			[HarmonyPrefix]
			public static bool Prefix(string path, ref string __result)
			{
				if (!Main.Settings.optimizer.customLevelReadOptimization)
					return true; // use original

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
						// "actions" came before "settings" - fallback to full parse
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
				catch (System.Exception e)
				{
					Main.Logger?.Error($"[JsonPatches] GetCustomLevelName failed: {e.Message}");
					return true; // fallback to original
				}
			}
		}

		[HarmonyPatch(typeof(LevelData), nameof(LevelData.Decode))]
		public static class ForceAngleDataPatch
		{
			public static void Prefix(Dictionary<string, object> dict)
			{
				if (dict is null) return;
				if (!Main.Settings.compatibility.forceAngleData) return;
				if (!dict.TryGetValue("pathData", out object val) || val is not string pathData) return;
				// for compatibility, convert pathData to angleData if not present. But: two methods exists and only one works in game,so uses string instaed nameof()

				var convertMethod = AccessTools.Method(typeof(scrLevelMaker), "StringToAngleArray");
				if (convertMethod != null)
				{
					dict["angleData"] = ((float[])convertMethod.Invoke(null, new object[] { pathData })).Cast<object>().ToList();
				}
				else
				{
					var migrateMethod = AccessTools.Method(typeof(FloorHelper), "MigratePathData");
					if (migrateMethod != null)
					{
						dict["angleData"] = ((float[])migrateMethod.Invoke(null, new object[] { pathData })).Cast<object>().ToList();
					}
					else
					{
						dict["angleData"] = pathData.Select(c => (object)(float)PathIdToRadiansSafe(c)).ToList();
					}
				}
				dict.Remove("pathData");
			}
		}

		[HarmonyPatch(typeof(LevelData), nameof(LevelData.Decode))]
		public static class LegacyBehaviorPatch
		{
			public static void Postfix(LevelData __instance)
			{
				var comp = Main.Settings.compatibility;
				// LegacyBehaviorMode.Default means "use the value from the JSON", so we only override if it's not Default.

				if (comp.legacyFlashMode != LegacyBehaviorMode.Default)
					__instance.legacyFlash = comp.legacyFlashMode == LegacyBehaviorMode.AlwaysOn;

				if (comp.legacyCamRelativeToMode != LegacyBehaviorMode.Default)
					__instance.legacyCamRelativeTo = comp.legacyCamRelativeToMode == LegacyBehaviorMode.AlwaysOn;
			}
		}
	}
}
