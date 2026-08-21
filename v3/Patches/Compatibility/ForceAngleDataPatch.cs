using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using ADOFAI;
using Iridium.Config;

namespace Iridium.Patches.Compatibility
{
	[IriPatch(Path = "compatibility/forceAngle", Pre = typeof(CompatibilitySettings), Condition = "forceAngleData")]
	[HarmonyPatch(typeof(LevelData), nameof(LevelData.Decode))]
	public static class ForceAngleDataPatch
	{
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

		public static void Prefix(Dictionary<string, object> dict)
		{
			if (dict is null) return;
			if (!Main.Settings.compatibility.forceAngleData) return;
			if (!dict.TryGetValue("pathData", out object val) || val is not string pathData) return;

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
}
