using System;
using System.Collections.Generic;
using ADOFAI;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using HarmonyLib;
using UnityEngine;

namespace Iridium.Patches
{
	public static class ExtremeOptimizationPatches
	{
		#region Configuration

		private const int MAX_TWEENS_PER_FRAME = 100;
		private const int BATCH_THRESHOLD = 50;
		private const float FRAME_SPREAD = 0.016f;

		#endregion

		#region Tween Batch Processing

		private static class TweenBatchQueue
		{
			private static readonly Queue<TweenRequest> _pendingTweens = new Queue<TweenRequest>();
			private static int _tweensCreatedThisFrame = 0;
			private static bool _isProcessing = false;

			public static void Enqueue(TweenRequest request)
			{
				_pendingTweens.Enqueue(request);
				if (!_isProcessing)
				{
					StartProcessing();
				}
			}

			public static void StartProcessing()
			{
				if (_isProcessing) return;
				_isProcessing = true;
				ProcessBatch();
			}

			private static void ProcessBatch()
			{
				_tweensCreatedThisFrame = 0;

				while (_pendingTweens.Count > 0 && _tweensCreatedThisFrame < MAX_TWEENS_PER_FRAME)
				{
					var request = _pendingTweens.Dequeue();
					request.Execute();
					_tweensCreatedThisFrame++;
				}

				if (_pendingTweens.Count > 0)
				{
					Main.Logger?.Log($"[ExtremeOpt] Deferred {_pendingTweens.Count} tweens to next frame");
				}
				else
				{
					_isProcessing = false;
				}
			}

			public static void Clear()
			{
				_pendingTweens.Clear();
				_isProcessing = false;
				_tweensCreatedThisFrame = 0;
			}

			public static int PendingCount => _pendingTweens.Count;
		}

		private abstract class TweenRequest
		{
			public abstract void Execute();
		}

		#endregion

		#region MoveTrack Extreme Optimization

		[HarmonyPatch(typeof(ffxMoveFloorPlus), nameof(ffxMoveFloorPlus.StartEffect))]
		public static class ExtremeMoveFloorPatch
		{
			public static bool Prefix(ffxMoveFloorPlus __instance)
			{
				if (!Main.Settings.optimizer.enableOptimizer ||
					!Main.Settings.optimizer.enableExtremeOptimization)
					return true;

				if (Main.Settings.optimizer.optimizeMoveTrack)
					return true;

				try
				{
					__instance.AdjustDurationForHardbake();

					int startIdx = __instance.start;
					int endIdx = __instance.end;
					if (endIdx < startIdx)
					{
						int temp = startIdx;
						startIdx = endIdx;
						endIdx = temp;
					}

					int floorCount = endIdx - startIdx + 1;
					bool isExtremeCase = floorCount > BATCH_THRESHOLD;

					if (!isExtremeCase)
						return true;

					Vector3 posOffset = new Vector3(__instance.targetPos.x, __instance.targetPos.y, 0f);
					float rotZ = __instance.targetRot;
					Vector3 scaleTarget = new Vector3(__instance.targetScaleV2.x, __instance.targetScaleV2.y, 1f);

					var floors = ADOBase.lm.listFloors;
					int count = floors.Count;
					int step = 1 + __instance.gapLength;

					ProcessExtremeMoveFloor(__instance, floors, startIdx, endIdx, step, count,
						posOffset, rotZ, scaleTarget);
					return false;
				}
				catch (Exception e)
				{
					Main.Logger?.Error($"[ExtremeOpt] MoveFloor failed: {e}");
					return true;
				}
			}

			private static void ProcessExtremeMoveFloor(ffxMoveFloorPlus instance, List<scrFloor> floors,
				int startIdx, int endIdx, int step, int count,
				Vector3 posOffset, float rotZ, Vector3 scaleTarget)
			{
				int totalFloors = (endIdx - startIdx) / step + 1;
				int floorsPerFrame = Mathf.Max(1, totalFloors / 10);

				int processed = 0;

				for (int i = startIdx; i <= endIdx && i < count; i += step)
				{
					scrFloor target = floors[i];
					Transform targetTransform = target.transform;
					var moveTweens = target.moveTweens;

					Vector3 targetPos = target.startPos + posOffset;
					float finalRotZ = (target.startRot + new Vector3(0, 0, rotZ)).z;

					float delay = (processed / floorsPerFrame) * FRAME_SPREAD;

					if (instance.positionUsed)
					{
						if (!float.IsNaN(targetPos.x))
						{
							if (moveTweens.TryGetValue(TweenType.PositionX, out var oldTx)) oldTx.Kill(true);
							if (!Mathf.Approximately(targetTransform.position.x, targetPos.x))
							{
								var tw = DOTween.To(
									() => targetTransform.position.x,
									x => targetTransform.MoveX(x),
									targetPos.x, instance.duration)
									.SetEase(instance.ease)
									.SetDelay(delay);
								moveTweens[TweenType.PositionX] = tw;
							}
						}
						if (!float.IsNaN(targetPos.y))
						{
							if (moveTweens.TryGetValue(TweenType.PositionY, out var oldTy)) oldTy.Kill(true);
							if (!Mathf.Approximately(targetTransform.position.y, targetPos.y))
							{
								var tw = DOTween.To(
									() => targetTransform.position.y,
									y => targetTransform.MoveY(y),
									targetPos.y, instance.duration)
									.SetEase(instance.ease)
									.SetDelay(delay);
								moveTweens[TweenType.PositionY] = tw;
							}
						}
					}

					if (instance.rotationUsed)
					{
						if (moveTweens.TryGetValue(TweenType.Rotation, out var oldTr)) oldTr.Kill(true);
						if (!Mathf.Approximately(targetTransform.eulerAngles.z, finalRotZ))
						{
							var tw = DOTween.To(
								() => target.tweenRot.z,
								r =>
								{
									target.tweenRot.z = r;
									targetTransform.eulerAngles = target.tweenRot;
								},
								finalRotZ, instance.duration)
								.SetEase(instance.ease)
								.SetDelay(delay);
							moveTweens[TweenType.Rotation] = tw;
						}
					}

					if (instance.scaleUsed)
					{
						if (!float.IsNaN(scaleTarget.x))
						{
							if (moveTweens.TryGetValue(TweenType.ScaleX, out var oldSx)) oldSx.Kill(true);
							var tw = targetTransform.DOScaleX(scaleTarget.x, instance.duration)
								.SetEase(instance.ease)
								.SetDelay(delay);
							moveTweens[TweenType.ScaleX] = tw;
						}
						if (!float.IsNaN(scaleTarget.y))
						{
							if (moveTweens.TryGetValue(TweenType.ScaleY, out var oldSy)) oldSy.Kill(true);
							var tw = targetTransform.DOScaleY(scaleTarget.y, instance.duration)
								.SetEase(instance.ease)
								.SetDelay(delay);
							moveTweens[TweenType.ScaleY] = tw;
						}
					}

					if (instance.opacityUsed)
					{
						if (moveTweens.TryGetValue(TweenType.Opacity, out var oldTo)) oldTo.Kill(true);
						if (!Mathf.Approximately(target.opacity, instance.targetOpacity))
						{
							var t = target.TweenOpacity(instance.targetOpacity, instance.duration, instance.ease);
							if (t != null)
							{
								t.SetDelay(delay);
								moveTweens[TweenType.Opacity] = t;
							}
						}
					}

					processed++;
				}

				Main.Logger?.Log($"[ExtremeOpt] Processed {processed} MoveFloor events in batches");
			}
		}

		#endregion

		#region MoveDecoration Extreme Optimization

		[HarmonyPatch(typeof(ffxMoveDecorationsPlus), nameof(ffxMoveDecorationsPlus.StartEffect))]
		public static class ExtremeMoveDecorPatch
		{
			public static bool Prefix(ffxMoveDecorationsPlus __instance)
			{
				if (!Main.Settings.optimizer.enableOptimizer ||
					!Main.Settings.optimizer.enableExtremeOptimization)
					return true;

				if (Main.Settings.optimizer.optimizeMoveDecorations)
					return true;

				try
				{
					__instance.AdjustDurationForHardbake();

					int decorCount = 0;
					if (__instance.targetTags != null)
					{
						foreach (string tag in __instance.targetTags)
						{
							if (__instance.decManager != null &&
							    __instance.decManager.taggedDecorations != null &&
							    __instance.decManager.taggedDecorations.ContainsKey(tag))
							{
								decorCount += __instance.decManager.taggedDecorations[tag].Count;
							}
						}
					}

					bool isExtremeCase = decorCount > BATCH_THRESHOLD;

					if (!isExtremeCase)
						return true;

					ProcessExtremeMoveDecor(__instance);
					return false;
				}
				catch (Exception e)
				{
					Main.Logger?.Error($"[ExtremeOpt] MoveDecor failed: {e}");
					return true;
				}
			}

			private static void ProcessExtremeMoveDecor(ffxMoveDecorationsPlus instance)
			{
				var decorManager = scnGame.suitableDecManager;
				if (decorManager == null) return;

				Vector3 targetPos = instance.targetPos;
				float duration = instance.duration;
				Ease ease = instance.ease;

				List<scrDecoration> allDecors = new List<scrDecoration>();
				foreach (string tag in instance.targetTags)
				{
					if (decorManager.taggedDecorations.TryGetValue(tag, out var taggedList))
					{
						foreach (var decor in taggedList)
						{
							if (!allDecors.Contains(decor))
							{
								allDecors.Add(decor);
							}
						}
					}
				}

				int totalDecors = allDecors.Count;
				int decorsPerFrame = Mathf.Max(1, totalDecors / 10);

				int processed = 0;

				foreach (var decor in allDecors)
				{
					if (decor == null || decor.transform == null) continue;

					float delay = (processed / decorsPerFrame) * FRAME_SPREAD;
					decor.transform.DOMove(targetPos, duration)
						.SetEase(ease)
						.SetDelay(delay);

					processed++;
				}

				Main.Logger?.Log($"[ExtremeOpt] Processed {processed} MoveDecor events in batches");
			}
		}

		#endregion

		#region Update Hook

		[HarmonyPatch(typeof(scnGame), "Update")]
		public static class ProcessPendingTweensPatch
		{
			[HarmonyPostfix]
			public static void Postfix()
			{
				if (!Main.Settings.optimizer.enableOptimizer) return;

				if (TweenBatchQueue.PendingCount > 0)
				{
					TweenBatchQueue.StartProcessing();
				}
			}
		}

		#endregion

		#region Cleanup

		[HarmonyPatch(typeof(scnGame), "OnDestroy")]
		public static class CleanupBatchQueuePatch
		{
			[HarmonyPostfix]
			public static void Postfix()
			{
				TweenBatchQueue.Clear();
				Main.Logger?.Log("[ExtremeOpt] Cleared batch queue");
			}
		}

		#endregion
	}
}