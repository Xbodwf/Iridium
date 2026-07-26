using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using ADOFAI;
using HarmonyLib;
using Iridium;
using Iridium.UI;
using UnityEngine;

namespace Iridium.Patches
{
	public static class MiscPatches
	{
		// 公共场景白名单，用于大厅相关功能
		private static readonly HashSet<string> LobbyScenes = new()
		{
			"scnLevelSelect",
			"scnCLS",
			"scnTaroMenu0",
			"scnTaroMenu1",
			"scnTaroMenu2",
			"scnTaroMenu3"
		};

		[HarmonyPatch(typeof(scnLevelSelect))]
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

		[HarmonyPatch(typeof(scrMisc), "DetermineDifficultyUIMode")]
		public static class ForceDifficultyUIPatch
		{
			public static void Postfix(ref DifficultyUIMode __result)
			{
				if (ADOBase.isCLSLevel) __result = DifficultyUIMode.ShowAll;
			}
		}

		[HarmonyPatch(typeof(FloorMesh), "SmallestAngleBetweenTwoAngles")]
		public static class CircleArcPatch
		{
			public static bool Prefix(float angleA, float angleB, ref float __result)
			{
				float minDiff = Mathf.Abs(Mathf.DeltaAngle(angleA * Mathf.Rad2Deg, angleB * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
				float minDiffDeg = minDiff * Mathf.Rad2Deg;
				if (minDiffDeg >= 89.9f && minDiffDeg <= 105.1f)
				{
					__result = minDiff * 5f / 180f * Mathf.PI;
					return false;
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(scrEnableIfBeta), "Awake")]
		public static class HideBetaWatermarkPatch
		{
			public static void Postfix(scrEnableIfBeta __instance)
			{
				if (Main.Settings.ui.hideBetaWatermark)
					__instance.gameObject.SetActive(false);
			}
		}

		public static void RefreshBetaWatermark()
		{
			var hide = Main.Settings.ui.hideBetaWatermark;
			foreach (var watermark in Resources.FindObjectsOfTypeAll<scrEnableIfBeta>())
			{
				watermark.gameObject.SetActive(!hide);
			}
		}

		[HarmonyPatch(typeof(scrUIController), "Update")]
		public static class AutoplayTextPositionPatch
		{
			[HarmonyPrefix]
			public static void Prefix()
			{
				RefreshAutoplayTextPosition();
			}
		}

		private static bool _isAutoplayModified = false;
		private static Vector3 _originalAutoplayPos;

		public static void RefreshAutoplayTextPosition()
		{
			if (scrUIController.instance?.txtDebug == null) return;

			// 仅在第一次修改前记录位置
			if (!_isAutoplayModified)
			{
				_originalAutoplayPos = scrUIController.instance.txtDebug.transform.localPosition;
				_isAutoplayModified = true;
			}

			scrUIController.instance.txtDebug.transform.localPosition = new Vector3(Main.Settings.ui.autoplayTextX, Main.Settings.ui.autoplayTextY, 0f);
		}

		[HarmonyPatch(typeof(scrConductor), "Update")]
		public static class CustomBpmPatch
		{
			[HarmonyPrefix]
			public static void Prefix()
			{
				UpdateBpm();
			}

			public static void UpdateBpm()
			{
				if (!Main.Settings.lobbyMusic.enableCustomBpm || scrConductor.instance is null) return;
				if (!LobbyScenes.Contains(ADOBase.sceneName)) return;

				scrConductor.instance.bpm = Main.Settings.lobbyMusic.customBpm;
			}
		}

		[HarmonyPatch(typeof(scnLevelSelect), "Awake")]
		public static class LobbyMusicPatch
		{
			private static bool _loadingDefault;
			private static bool _loadingFast;
			private static AudioClip? _defaultBgm;
			private static AudioClip? _fastBgm;

			[HarmonyPostfix]
			public static void Postfix()
			{
				ReloadFromSettings();
			}

			public static void ReloadFromSettings()
			{
				if (!Main.Settings.lobbyMusic.customMusic)
				{
					TryApplyLoadedClips();
					return;
				}

				StartLoad(true, Main.Settings.lobbyMusic.defaultMusicPath);
				StartLoad(false, Main.Settings.lobbyMusic.fastMusicPath);
			}

			public static void StartLoad(bool loadDefault, string? path)
			{
				if (scrConductor.instance is null) return;
				scrConductor.instance.StartCoroutine(LoadMusicCo(loadDefault, path));
			}

			private static IEnumerator LoadMusicCo(bool loadDefault, string? path)
			{
				if (loadDefault)
				{
					_loadingDefault = true;
					_defaultBgm = null;
				}
				else
				{
					_loadingFast = true;
					_fastBgm = null;
				}

				AudioClip? clip = null;
				if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
				{
					Main.Logger?.Log($"[LobbyMusic] start loading '{path}', default={loadDefault}");

					clip = AudioManager.Instance.FindOrLoadAudioClip(Path.GetFileName(path) + "*external", null);
					if (clip == null)
					{
						IEnumerator load = AudioManager.Instance.FindOrLoadAudioClipExternal(path, false, 0f);
						yield return load;
						RDAudioLoadResult result = (RDAudioLoadResult)load.Current;
						if ((int)result.type == 0)
						{
							clip = result.clip;
						}
						else
						{
							Main.Logger?.Log($"[LobbyMusic] load failed: {result.type}");
						}
					}

					Main.Logger?.Log($"[LobbyMusic] end loading '{path}', default={loadDefault}");
				}

				if (loadDefault)
				{
					_loadingDefault = false;
					_defaultBgm = clip;
				}
				else
				{
					_loadingFast = false;
					_fastBgm = clip;
				}

				TryApplyLoadedClips();
			}

			public static void TryApplyLoadedClips()
			{
				if (scrConductor.instance is null || !ADOBase.isLevelSelect) return;

				if (!Main.Settings.lobbyMusic.customMusic)
				{
					return;
				}

				bool fast = Main.Settings.lobbyMusic.fastMusic;

				if (!_loadingDefault)
				{
					if ((scrConductor.instance.song.clip = _defaultBgm) is null)
					{
						scrConductor.instance.song.Stop();
					}
					else
					{
						scrConductor.instance.song.volume = 1f;
						scrConductor.instance.song.pitch = 1f;
						scrConductor.instance.song.Stop();
						if (!fast) scrConductor.instance.song.Play();
					}
				}

				if (!_loadingFast)
				{
					if ((scrConductor.instance.song2.clip = _fastBgm) is null)
					{
						scrConductor.instance.song2.Stop();
					}
					else
					{
						scrConductor.instance.song2.pitch = 1f;
						scrConductor.instance.song2.Stop();
						if (fast) scrConductor.instance.song2.Play();

						// 确保只有一个 AudioSource 有声
						scrConductor.instance.song.volume = fast ? 0f : 1f;
						scrConductor.instance.song2.volume = fast ? 1f : 0f;
					}
				}
			}
		}

		[HarmonyPatch(typeof(scnGame), "Play")]
		public static class AlwaysCountdownPatch
		{
			private static bool _tempAuto;

			public static void Prefix()
			{
				if (!Main.Settings.ui.alwaysCountdown || !ADOBase.isLevelEditor) return;
				_tempAuto = RDC.auto;
				RDC.auto = false;
			}

			public static void Postfix()
			{
				if (!Main.Settings.ui.alwaysCountdown || !ADOBase.isLevelEditor) return;
				RDC.auto = _tempAuto;
			}
		}

		[HarmonyPatch(typeof(scnEditor), "Update")]
		public static class AutoplayHintUIPatch
		{
			[HarmonyPostfix]
			public static void Postfix(scnEditor __instance)
			{
				if (!RDC.auto || !__instance.playMode) return;
				if (!Main.Settings.ui.showAutoplayHintUI)
				{
					__instance.controlsTip.text = "";
				}
				else if (Main.Settings.ui.customAutoplayHint)
				{
					__instance.controlsTip.text = string.IsNullOrEmpty(Main.Settings.ui.autoplayHintTemplate)
						? ""
						: ProcessAutoplayHintTemplate(Main.Settings.ui.autoplayHintTemplate, __instance.pausedInPlayMode);
				}
			}

			private static string ProcessAutoplayHintTemplate(string template, bool paused)
			{
				var autoplayText = RDString.Get("status.autoplay");
				var statusText = paused ? RDString.Get("status.paused") : Localization.Get("AutoplayStatusEnabled");
				var keyCode = Main.Settings.compatibility.editorPauseAllowed && Main.Settings.compatibility.editorPauseEnabled
					? Main.Settings.compatibility.editorPauseKey
					: 32;
				var keyName = GetKeyDisplayName(keyCode);
				return template
					.Replace("{autoplay}", autoplayText)
					.Replace("{status}", statusText)
					.Replace("{key}", keyName);
			}

			private static string GetKeyDisplayName(int keyCode)
			{
				switch ((KeyCode)keyCode)
				{
					case KeyCode.Space: return "Space";
					case KeyCode.Return: return "Enter";
					case KeyCode.Escape: return "Esc";
					case KeyCode.Tab: return "Tab";
					case KeyCode.LeftShift: return "L-Shift";
					case KeyCode.RightShift: return "R-Shift";
					case KeyCode.LeftControl: return "L-Ctrl";
					case KeyCode.RightControl: return "R-Ctrl";
					case KeyCode.LeftAlt: return "L-Alt";
					case KeyCode.RightAlt: return "R-Alt";
					default:
						if (keyCode >= (int)KeyCode.A && keyCode <= (int)KeyCode.Z)
							return ((char)('A' + (keyCode - (int)KeyCode.A))).ToString();
						if (keyCode >= (int)KeyCode.Alpha0 && keyCode <= (int)KeyCode.Alpha9)
							return ((char)('0' + (keyCode - (int)KeyCode.Alpha0))).ToString();
						if (keyCode >= (int)KeyCode.F1 && keyCode <= (int)KeyCode.F12)
							return "F" + (keyCode - (int)KeyCode.F1 + 1);
						return ((KeyCode)keyCode).ToString();
				}
			}
		}
	}
}
