using System;
using HarmonyLib;
using Iridium.Config;
using TMPro;
using DG.Tweening;
using UnityEngine;

namespace Iridium.Patches
{
	public static class JudgeTextPatches
	{
		private static JudgeTextSettings Settings => Main.Settings.judgeText;

		// Captured missAngle from scrHitTextManager.ShowHitText (game doesn't forward it to Show in non-coop)
		private static float _capturedMissAngle;

		private static double CalculateTimingFromAngle(float angularOffset)
		{
			var controller = scrController.instance;
			var conductor = scrConductor.instance;
			if (controller == null || conductor == null) return 0;

			double bpm = conductor.bpm;
			double speed = controller.d_speed;
			double pitch = conductor.song.pitch;

			double standardTiming = angularOffset * (controller.playerOne.planetarySystem.isCW ? 1.0 : -1.0) * 60000.0 / (Math.PI * bpm * speed * pitch);

			return -standardTiming;
		}

		[HarmonyPatch(typeof(scrHitTextManager), "ShowHitText")]
		public static class HitTextManagerShowPatch
		{
			public static void Prefix(float missAngle)
			{
				_capturedMissAngle = missAngle;
			}
		}

		[HarmonyPatch(typeof(scrHitTextMesh), "Init")]
		public static class HitTextMeshInitPatch
		{
			public static void Postfix(scrHitTextMesh __instance, HitMargin hitMargin, TextMeshPro ___text)
			{
				if (!Settings.enableJudgeTextCustomization)
				{
					___text.text = hitMargin.ToString();
					return;
				}

				___text.text = Settings.GetTextForHitMargin((int)hitMargin);
			}
		}

		[HarmonyPatch(typeof(scrHitTextMesh), "Show")]
		public static class HitTextMeshShowPatch
		{
			public static void Prefix(scrHitTextMesh __instance, TextMeshPro ___text)
			{
				if (!Settings.enableJudgeTextCustomization) return;
				if (___text == null) return;

				string template = Settings.GetTextForHitMargin((int)__instance.hitMargin);
				double timing = CalculateTimingFromAngle(_capturedMissAngle);
				___text.text = JudgeTextSettings.ReplaceOffset(template, timing);
			}
		}

		[HarmonyPatch(typeof(scrHitTextMesh), "Show")]
		public static class HitTextMeshShowRotationFixPatch
		{
			public static void Postfix(scrHitTextMesh __instance)
			{
				if (scrController.coopMode) return;
				if (__instance.hitMargin == HitMargin.Perfect) return;

				__instance.transform.DOLocalRotate(
					new Vector3(0f, 0f, _capturedMissAngle * 20f),
					2f,
					RotateMode.LocalAxisAdd
				);
			}
		}
	}
}
