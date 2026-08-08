using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using ADOFAI;

namespace Iridium.Patches
{
	public static class MultithreadOptimizationPatches
	{
		private static readonly object _taskLock = new();
		private static readonly List<Task> _runningTasks = new();

		#region Parallel Texture Processing

		public static void ProcessTexturesParallel(List<Texture2D> textures, double divideBy)
		{
			if (textures == null || textures.Count == 0) return;

			Parallel.ForEach(textures, texture =>
			{
				if (texture == null) return;

				try
				{
					int targetW = Mathf.Max(4, (int)(texture.width / divideBy));
					int targetH = Mathf.Max(4, (int)(texture.height / divideBy));

					var resized = OptimizerPatches.CreateProcessedTexture(texture, targetW, targetH);
					if (resized != null)
					{
						Main.DestroyImmediate(texture);
					}
				}
				catch (Exception e)
				{
					Main.Logger?.Error($"[Multithread] Texture processing failed: {e}");
				}
			});
		}

		#endregion

		#region Parallel Math Calculations

		public static class DecorationCalculationCache
		{
			private struct DecorationTransform
			{
				public Vector3 position;
				public Quaternion rotation;
				public Vector3 scale;
			}

			private static readonly Dictionary<int, DecorationTransform> _cache = new();
			private static int _lastFrame = -1;

			public static void PreCalculateTransforms(List<scrDecoration> decorations, float deltaTime)
			{
				if (decorations == null || decorations.Count == 0) return;

				int currentFrame = Time.frameCount;
				if (currentFrame == _lastFrame) return;
				_lastFrame = currentFrame;

				Task.Run(() =>
				{
					try
					{
						Parallel.For(0, decorations.Count, i =>
						{
							var decor = decorations[i];
							if (decor == null) return;

							var transform = new DecorationTransform
							{
								position = Vector3.zero,
								rotation = Quaternion.identity,
								scale = Vector3.one
							};

							lock (_cache)
							{
								_cache[decor.GetInstanceID()] = transform;
							}
						});
					}
					catch (Exception e)
					{
						Main.Logger?.Error($"[Multithread] Transform calculation failed: {e}");
					}
				});
			}

			public static bool TryGetCachedTransform(int instanceId, out Vector3 position, out Quaternion rotation, out Vector3 scale)
			{
				lock (_cache)
				{
					if (_cache.TryGetValue(instanceId, out var transform))
					{
						position = transform.position;
						rotation = transform.rotation;
						scale = transform.scale;
						return true;
					}
				}

				position = Vector3.zero;
				rotation = Quaternion.identity;
				scale = Vector3.one;
				return false;
			}

			public static void Clear()
			{
				lock (_cache)
				{
					_cache.Clear();
				}
			}
		}

		#endregion

		#region Parallel Event Processing

		[HarmonyPatch(typeof(scnGame), "ApplyEventsToFloors",
			typeof(List<scrFloor>), typeof(LevelData), typeof(scrLevelMaker), typeof(List<LevelEvent>))]
		public static class ParallelEventProcessingPatch
		{
			private static readonly Dictionary<int, List<LevelEvent>> _floorEventCache = new();

			[HarmonyPrefix]
			public static void Prefix(List<scrFloor> floors, List<LevelEvent> events)
			{
				if (!Main.Settings.optimizer.enableOptimizer) return;

				Task.Run(() =>
				{
					try
					{
						var tempCache = new Dictionary<int, List<LevelEvent>>();

						Parallel.ForEach(events, evt =>
						{
							if (evt.floor < 0 || evt.floor >= floors.Count) return;

							lock (tempCache)
							{
								if (!tempCache.TryGetValue(evt.floor, out var list))
								{
									list = new List<LevelEvent>();
									tempCache[evt.floor] = list;
								}
								list.Add(evt);
							}
						});

						lock (_floorEventCache)
						{
							_floorEventCache.Clear();
							foreach (var kvp in tempCache)
							{
								_floorEventCache[kvp.Key] = kvp.Value;
							}
						}
					}
					catch (Exception e)
					{
						Main.Logger?.Error($"[Multithread] Event processing failed: {e}");
					}
				});
			}

			public static List<LevelEvent>? GetCachedEvents(int floorIndex)
			{
				lock (_floorEventCache)
				{
					return _floorEventCache.TryGetValue(floorIndex, out var list) ? list : null;
				}
			}
		}

		#endregion

		#region Cleanup

		public static void Cleanup()
		{
			lock (_taskLock)
			{
				foreach (var task in _runningTasks)
				{
					try
					{
						if (!task.IsCompleted)
						{
							task.Wait(1000);
						}
					}
					catch (Exception e)
					{
						Main.Logger?.Error($"[Multithread] Task cleanup error: {e}");
					}
				}
				_runningTasks.Clear();
			}

			DecorationCalculationCache.Clear();
			Main.Logger?.Log("[Multithread] Cleanup completed");
		}

		#endregion
	}
}
