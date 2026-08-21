using Iridium.Config;
using HarmonyLib;

namespace Iridium.Patches.UI
{
	[HarmonyPatch(typeof(PausePlanets), "UpdateParticles")]
	[IriPatch(Path = "ui/pauseTrail", Pre = typeof(UISettings), Condition = "enablePausePlanetTrail")]
	public static class PausePlanetTrailPatch
	{
		[HarmonyPrefix]
		public static bool Prefix(bool show)
		{
			if (!show) return false;
			return true;
		}
	}
}
