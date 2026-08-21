using Iridium.Config;
using HarmonyLib;
using ADOFAI;

namespace Iridium.Patches.Compatibility
{
	[HarmonyPatch(typeof(scrDecoration), nameof(scrDecoration.HitboxTriggerAction))]
	[IriPatch(Path = "compatibility/noFail", Pre = typeof(CompatibilitySettings), Condition = "enableNoFailTooEarly")]
	public static class NoFailTooEarlyPatch
	{
		public static void Prefix(scrDecoration __instance, out HitboxType __state, scrPlanet planet)
		{
			__state = __instance.hitbox;
			if (!ADOBase.controller.gameworld || !ADOBase.controller.noFail || __instance.hitbox != HitboxType.Kill)
			{
				return;
			}

			if (RDC.auto)
			{
				return;
			}

			__instance.hitbox = HitboxType.None;
			if ((planet != null && planet.iFrames > 0) || __instance.hitOnce)
			{
				return;
			}

			ADOBase.controller.playerOne.marginTracker.AddHit(HitMargin.FailOverload);
			ADOBase.controller.errorMeter?.AddHit(float.NegativeInfinity);
			ADOBase.controller.chosenPlanet.MarkFail()?.BlinkForSeconds(3);
		}

		public static void Postfix(scrDecoration __instance, HitboxType __state)
		{
			__instance.hitbox = __state;
		}
	}
}
