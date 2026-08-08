using HarmonyLib;

namespace Iridium.Patches
{
	/// <summary>
	/// Keeps planet particles alive during pause-menu option switching so the
	/// planets show their motion trail instead of appearing as a solid ball.
	/// </summary>
	[HarmonyPatch(typeof(PausePlanets), "UpdateParticles")]
	public static class PausePlanetTrailPatch
	{
		[HarmonyPrefix]
		public static bool Prefix(bool show)
		{
			if (!show) return false; // skip DisableParticles, keep trail alive
			return true;
		}
	}
}
