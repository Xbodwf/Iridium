using HarmonyLib;
using ADOFAI;

namespace Iridium.Patches.Bugfix
{
	[HarmonyPatch(typeof(scnGame), nameof(scnGame.Play))]
	[IriPatch(Path = "bugfix", AlwaysOn = true)]
	public static class AsyncInputPlaySnapPatch
	{
		[HarmonyPrefix]
		public static void Prefix()
		{
			if (AsyncInputManager.isActive)
				AsyncInputUtils.UpdateOffsetTime(1L);
		}
	}
}
