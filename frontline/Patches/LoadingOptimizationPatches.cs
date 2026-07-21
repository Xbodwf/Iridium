using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using ADOFAI;
using DG.Tweening;
using Iridium.UI;

namespace Iridium.Patches
{
	public static class LoadingOptimizationPatches
	{
		#region Shared State

		private static readonly Stack<Tween> _tweenPool = new(100);
		private static readonly object _tweenPoolLock = new();

		private static bool _isBatchCreating = false;

		private static Dictionary<int, List<LevelEvent>>? _floorEventsCache = null;

		#endregion

		#region Event Processing Optimization

		[HarmonyPatch(typeof(scnGame), "ApplyEventsToFloors",
			typeof(List<scrFloor>), typeof(LevelData), typeof(scrLevelMaker), typeof(List<LevelEvent>))]
		public static class EventPreprocessingPatch
		{
			[HarmonyPrefix]
			public static void Prefix(List<scrFloor> floors, List<LevelEvent> events)
			{
				if (!Main.Settings.optimizer.cacheFloorEvents) return;

				try
				{
					_floorEventsCache = new Dictionary<int, List<LevelEvent>>(floors.Count);

					foreach (var evt in events)
					{
						int floorIndex = evt.floor;
						if (floorIndex < 0 || floorIndex >= floors.Count) continue;

						if (!_floorEventsCache.TryGetValue(floorIndex, out var list))
						{
							list = new List<LevelEvent>();
							_floorEventsCache[floorIndex] = list;
						}

						list.Add(evt);
					}

					Main.Logger?.Log($"[LoadingOptimization] Cached events for {_floorEventsCache.Count} floors");
				}
				catch (Exception e)
				{
					Main.Logger?.Error($"[LoadingOptimization] Event preprocessing failed: {e}");
					_floorEventsCache = null;
				}
			}

			[HarmonyPostfix]
			public static void Postfix()
			{
				if (!Main.Settings.optimizer.cacheFloorEvents) return;

				_floorEventsCache = null;
			}

			public static List<LevelEvent>? GetCachedEvents(int floorIndex)
			{
				return _floorEventsCache?.TryGetValue(floorIndex, out var list) == true ? list : null;
			}
		}

		#endregion

		#region Tween Pool

		public static class TweenPoolManager
		{
			public static Tween? GetTween()
			{
				lock (_tweenPoolLock)
				{
					if (_tweenPool.Count > 0)
					{
						return _tweenPool.Pop();
					}
				}

				return null;
			}

			public static void ReturnTween(Tween tween)
			{
				if (tween == null) return;

				try
				{
					tween.Kill(false);

					lock (_tweenPoolLock)
					{
						if (_tweenPool.Count < 1000)
						{
							_tweenPool.Push(tween);
						}
					}
				}
				catch (Exception e)
				{
					Main.Logger?.Error($"[LoadingOptimization] Failed to return tween: {e}");
				}
			}

			public static void ClearPool()
			{
				lock (_tweenPoolLock)
				{
					while (_tweenPool.Count > 0)
					{
						var tween = _tweenPool.Pop();
						tween?.Kill();
					}
				}
			}
		}

		#endregion

		#region Frame-Spread Decoration Loading

		[HarmonyPatch(typeof(scnGame), "UpdateDecorationObjects")]
		public static class FrameSpreadDecorationLoadingPatch
		{
			private static readonly Queue<LevelEvent> _pendingDecorations = new();
			private static bool _isLoading = false;
			private static readonly List<GraphicRaycaster> _disabledRaycasters = new();
			private static bool _cancelled = false;
			private static scnGame? _pendingGame;
			private static bool _playWasBlocked;
			private static bool _uiCompleted;

			public static bool IsLoading => _isLoading;

			private const float TIME_BUDGET_PER_FRAME = 0.012f;

			[HarmonyPrefix]
			public static bool Prefix(scnGame __instance, bool reloadDecorations)
			{
				if (!Main.Settings.optimizer.enableOptimizer || !Main.Settings.optimizer.frameSpreadDecorationLoading)
					return true;

				if (_isLoading) return false;

				if (ADOBase.isOfficialLevel) return true;

				if (!reloadDecorations) return true;

				try
				{
					var decorations = __instance.decorations;
					if (decorations == null || decorations.Count == 0)
						return true;

					int totalActive = 0;
					foreach (var dec in decorations)
					{
						if (dec.active) totalActive++;
					}

					if (totalActive < 100)
						return true;

					Main.Logger?.Log($"[LoadingOptimization] Frame-spread loading {totalActive} decorations ({decorations.Count} total)");

					_isLoading = true;
					_pendingDecorations.Clear();
					_disabledRaycasters.Clear();

					foreach (var dec in decorations)
					{
						if (dec.active)
							_pendingDecorations.Enqueue(dec);
					}

					BlockUIInput();

					_pendingGame = __instance;
					__instance.StartCoroutine(FrameSpreadLoadCoroutine(__instance));
					return false;
				}
				catch (System.Exception ex)
				{
					Main.Logger?.Error($"[LoadingOptimization] FrameSpreadDecorationLoading failed: {ex}");
					CleanupState();
					return true;
				}
			}

			private static void BlockUIInput()
			{
				try
				{
					var canvases = Resources.FindObjectsOfTypeAll<Canvas>();
					foreach (var canvas in canvases)
					{
						var raycaster = canvas.GetComponent<GraphicRaycaster>();
						if (raycaster != null && raycaster.enabled)
						{
							raycaster.enabled = false;
							_disabledRaycasters.Add(raycaster);
						}
					}
					Main.Logger?.Log($"[LoadingOptimization] Blocked UI input: disabled {_disabledRaycasters.Count} raycaster(s)");
				}
				catch (Exception ex)
				{
					Main.Logger?.Error($"[LoadingOptimization] Failed to block UI input: {ex}");
				}
			}

			private static void RestoreUIInput()
			{
				try
				{
					foreach (var raycaster in _disabledRaycasters)
					{
						if (raycaster != null)
							raycaster.enabled = true;
					}
					_disabledRaycasters.Clear();
				}
				catch (Exception ex)
				{
					Main.Logger?.Error($"[LoadingOptimization] Failed to restore UI input: {ex}");
				}
			}

			public static void Cancel()
			{
				_cancelled = true;
				UI.VRAMNotificationUI.Complete(forceImmediate: true);
			}

			private static System.Collections.IEnumerator FrameSpreadLoadCoroutine(scnGame instance)
			{
				int maxPerFrame = Main.Settings.optimizer.decorationsPerFrame;
				if (maxPerFrame < 1) maxPerFrame = 50;

				if (instance == null || instance.decManager == null)
				{
					CleanupState();
					yield break;
				}

				instance.decManager.ClearDecorations();

				int processed = 0;
				int total = _pendingDecorations.Count;

				UI.VRAMNotificationUI.ShowPersistent(Localization.Get("LoadingDecorationsProgress", 0, total));
				Main.Logger?.Log($"[LoadingOptimization] Starting frame-spread loading: {total} decorations");

				while (_pendingDecorations.Count > 0 && !_cancelled)
				{
					if (instance == null || instance.decManager == null)
					{
						Main.Logger?.Log($"[LoadingOptimization] scnGame destroyed during loading, aborting");
						CleanupState();
						yield break;
					}

					float frameStart = Time.realtimeSinceStartup;
					int batchLimit = Mathf.Min(maxPerFrame, _pendingDecorations.Count);

					for (int i = 0; i < batchLimit && _pendingDecorations.Count > 0 && !_cancelled; i++)
					{
						var ev = _pendingDecorations.Dequeue();
						try
						{
							bool spritesLoaded = false;
							instance.decManager.CreateDecoration(ev, out spritesLoaded);
						}
						catch (System.Exception ex)
						{
							Main.Logger?.Error($"[LoadingOptimization] Failed to create decoration: {ex}");
						}
						processed++;

						if (Time.realtimeSinceStartup - frameStart > TIME_BUDGET_PER_FRAME)
							break;
					}

					if (_pendingDecorations.Count > 0 && !_cancelled)
					{
						UI.VRAMNotificationUI.UpdateProgress(Localization.Get("LoadingDecorationsProgress", processed, total));
						yield return null;
					}
				}

				if (_cancelled)
				{
					Main.Logger?.Log($"[LoadingOptimization] Loading cancelled by user");
					CleanupState();
					yield break;
				}

				var moveDecImages = new List<(string name, string path)>();
				foreach (var evt in instance.events)
				{
					if (evt.eventType != LevelEventType.MoveDecorations) continue;
					try
					{
						string? output2 = null;
						if (evt.TryGetAndSet("decorationImage", ref output2, onlyIfEnabled: true) && !output2.IsNullOrEmpty())
						{
							string filePath2 = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(instance.levelPath), output2!);
							moveDecImages.Add((output2!, filePath2));
						}
					}
					catch (Exception ex)
					{
						Main.Logger?.Error($"[LoadingOptimization] Failed to collect MoveDecoration image: {ex}");
					}
				}

				if (_cancelled)
				{
					Main.Logger?.Log($"[LoadingOptimization] Loading cancelled by user");
					CleanupState();
					yield break;
				}

				if (moveDecImages.Count > 0)
				{
					total += moveDecImages.Count;
					for (int i = 0; i < moveDecImages.Count; i++)
					{
						if (_cancelled)
						{
							Main.Logger?.Log($"[LoadingOptimization] Loading cancelled by user");
							UI.VRAMNotificationUI.Show(Localization.Get("LoadingDecorationsProgress", processed, total));
							_uiCompleted = true;
							CleanupState();
							yield break;
						}
						var (name, path) = moveDecImages[i];
						UI.VRAMNotificationUI.UpdateProgress(Localization.Get("LoadingDecorationsProgress", processed, total));
						try
						{
							LoadResult status;
							instance.imgHolder.GetOrAddSprite(name, path, out status);
							if (ADOBase.editor != null)
								ADOBase.editor.UpdateImageLoadResult(name, status);
						}
						catch (System.Exception ex)
						{
							Main.Logger?.Error($"[LoadingOptimization] Failed to load MoveDecoration image: {ex}");
						}
						processed++;
						yield return null;
					}
				}

				Main.Logger?.Log($"[LoadingOptimization] Finished loading {processed} decorations across multiple frames");

				if (!Main.Settings.optimizer.dontShowSavedMemory)
				{
					if (OptimizerPatches.savedVRAM_MB > 0.1f)
					{
						UI.VRAMNotificationUI.Show(Localization.Get("SavedMemoryMsg", OptimizerPatches.savedVRAM_MB.ToString("F2")));
						Main.Logger?.Log(Localization.Get("SavedMemoryLog", OptimizerPatches.savedVRAM_MB.ToString("F2")));
					}
					else
					{
						UI.VRAMNotificationUI.Show(Localization.Get("LoadingDecorationsProgress", processed, total));
					}
					_uiCompleted = true;
					OptimizerPatches.VRAMNotificationPatch.isFinished = true;
				}
				else
				{
					UI.VRAMNotificationUI.Show(Localization.Get("LoadingDecorationsProgress", processed, total));
					_uiCompleted = true;
				}

				var gameToPlay = (_pendingGame != null && _playWasBlocked) ? _pendingGame : null;
				CleanupState();
				if (instance != null && instance.decManager != null)
					instance.decManager.ResetDecorations();
				gameToPlay?.Play();
			}

			[HarmonyPatch(typeof(scrDecorationManager), "ResetDecorations")]
			public static class ResetDecorations_Patch
			{
				[HarmonyPrefix]
				public static bool Prefix()
				{
					if (_isLoading) return false;
					return true;
				}
			}

			[HarmonyPatch(typeof(scnGame), "Play",
				new Type[] { typeof(int), typeof(bool) })]
			public static class Play_Patch
			{
				[HarmonyPrefix]
				public static bool Prefix()
				{
					if (_isLoading)
					{
						_playWasBlocked = true;
						return false;
					}
					return true;
				}
			}

			private static void CleanupState()
			{
				_isLoading = false;
				_cancelled = false;
				_pendingGame = null;
				_playWasBlocked = false;
				_pendingDecorations.Clear();
				RestoreUIInput();
				if (!_uiCompleted)
					UI.VRAMNotificationUI.Complete();
				_uiCompleted = false;
			}
		}

		#endregion

		#region Cleanup

		[HarmonyPatch(typeof(scnGame), "OnDestroy")]
		public static class LoadingOptimizationCleanupPatch
		{
			[HarmonyPostfix]
			public static void Postfix()
			{
				if (!Main.Settings.optimizer.enableOptimizer) return;

				_floorEventsCache?.Clear();
				_floorEventsCache = null;
				_isBatchCreating = false;

				TweenPoolManager.ClearPool();

				Main.Logger?.Log("[LoadingOptimization] Cleaned up caches and pools");
			}
		}

		#endregion

		#region Utility Methods

		public static bool IsBatchCreating => _isBatchCreating;

		public static List<LevelEvent>? GetFloorEvents(int floorIndex)
		{
			return EventPreprocessingPatch.GetCachedEvents(floorIndex);
		}

		#endregion
	}
}
