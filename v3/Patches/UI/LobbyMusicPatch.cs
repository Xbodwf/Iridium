using Iridium.Config;
using System;
using System.Collections;
using System.IO;
using ADOFAI;
using UnityEngine;
using HarmonyLib;
using Iridium;

namespace Iridium.Patches.UI
{
	[HarmonyPatch(typeof(scnLevelSelect), "Awake")]
	[IriPatch(Path = "ui/lobbyMusic", Pre = typeof(LobbyMusicSettings), Condition = "enableLobbyMusicPatch")]
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

					scrConductor.instance.song.volume = fast ? 0f : 1f;
					scrConductor.instance.song2.volume = fast ? 1f : 0f;
				}
			}
		}
	}
}
