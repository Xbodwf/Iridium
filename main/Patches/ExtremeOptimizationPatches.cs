using System;
using System.Collections.Generic;
using ADOFAI;
using DG.Tweening;
using HarmonyLib;
using UnityEngine;

namespace Iridium.Patches
{
	/// <summary>
	/// 极端情况优化补丁 - 针对大量并发事件（如14万事件，12万MoveTrack/MoveDecoration）
	/// 通过分帧处理、批量创建、延迟启动等方式避免单帧过载
	/// </summary>
	public static class ExtremeOptimizationPatches
	{
		#region Configuration

		private const int MAX_TWEENS_PER_FRAME = 100; // 每帧最多创建的Tween数量
		private const int BATCH_THRESHOLD = 50; // 触发批量处理的事件数量阈值
		private const float FRAME_SPREAD = 0.016f; // 事件分散到多少帧内（约1帧）

		#endregion

		#region Tween Batch Processing

		/// <summary>
		/// Tween批处理队列 - 用于延迟创建Tween
		/// </summary>
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
					// 还有未处理的Tween，下一帧继续
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

		/// <summary>
		/// Tween请求 - 封装Tween创建逻辑
		/// </summary>
		private abstract class TweenRequest
		{
			public abstract void Execute();
		}

		private class MoveFloorRequest : TweenRequest
		{
			private readonly Transform _transform;
			private readonly float _targetX;
			private readonly float _targetY;
			private readonly float _targetRotZ;
			private readonly float _targetScaleX;
			private readonly float _targetScaleY;
			private readonly float _duration;
			private readonly Ease _ease;
			private readonly bool _positionUsed;
			private readonly bool _rotationUsed;
			private readonly bool _scaleUsed;

			public MoveFloorRequest(Transform transform, float targetX, float targetY, float targetRotZ,
				float targetScaleX, float targetScaleY, float duration, Ease ease,
				bool positionUsed, bool rotationUsed, bool scaleUsed)
			{
				_transform = transform;
				_targetX = targetX;
				_targetY = targetY;
				_targetRotZ = targetRotZ;
				_targetScaleX = targetScaleX;
				_targetScaleY = targetScaleY;
				_duration = duration;
				_ease = ease;
				_positionUsed = positionUsed;
				_rotationUsed = rotationUsed;
				_scaleUsed = scaleUsed;
			}

			public override void Execute()
			{
				try
				{
					if (_positionUsed)
					{
						if (!Mathf.Approximately(_transform.position.x, _targetX))
							DOTween.To(() => _transform.position.x, x => _transform.position = new Vector3(x, _transform.position.y, _transform.position.z),
								_targetX, _duration).SetEase(_ease);
						if (!Mathf.Approximately(_transform.position.y, _targetY))
							DOTween.To(() => _transform.position.y, y => _transform.position = new Vector3(_transform.position.x, y, _transform.position.z),
								_targetY, _duration).SetEase(_ease);
					}

					if (_rotationUsed)
					{
						if (!Mathf.Approximately(_transform.eulerAngles.z, _targetRotZ))
							_transform.DORotate(new Vector3(0, 0, _targetRotZ), _duration).SetEase(_ease);
					}

					if (_scaleUsed)
					{
						if (!Mathf.Approximately(_transform.localScale.x, _targetScaleX))
							_transform.DOScaleX(_targetScaleX, _duration).SetEase(_ease);
						if (!Mathf.Approximately(_transform.localScale.y, _targetScaleY))
							_transform.DOScaleY(_targetScaleY, _duration).SetEase(_ease);
					}
				}
				catch (Exception e)
				{
					Main.Logger?.Error($"[ExtremeOpt] MoveFloorRequest failed: {e}");
				}
			}
		}

		#endregion

		#region MoveTrack Extreme Optimization

		/// <summary>
		/// 极端优化版的MoveFloor - 检测到大量事件时分帧处理
		/// </summary>
		[HarmonyPatch(typeof(ffxMoveFloorPlus), nameof(ffxMoveFloorPlus.StartEffect))]
		public static class ExtremeMoveFloorPatch
		{
			public static bool Prefix(ffxMoveFloorPlus __instance)
			{
				if (!Main.Settings.optimizer.enableOptimizer ||
					!Main.Settings.optimizer.optimizeMoveTrack)
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

					// 检测是否是极端情况（大量并发事件）
					bool isExtremeCase = floorCount > BATCH_THRESHOLD;

					Vector3 posOffset = new Vector3(__instance.targetPos.x, __instance.targetPos.y, 0f);
					float rotZ = __instance.targetRot;
					Vector3 scaleTarget = new Vector3(__instance.targetScaleV2.x, __instance.targetScaleV2.y, 1f);

					var floors = ADOBase.lm.listFloors;
					int count = floors.Count;
					int step = 1 + __instance.gapLength;

					if (isExtremeCase)
					{
						// 极端情况：分帧处理
						ProcessExtremeMoveFloor(__instance, floors, startIdx, endIdx, step, count,
							posOffset, rotZ, scaleTarget);
						return false;
					}
					else
					{
						// 普通情况：立即处理（使用现有的优化）
						return true;
					}
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
				// 计算每帧处理多少个floor
				int totalFloors = (endIdx - startIdx) / step + 1;
				int floorsPerFrame = Mathf.Min(MAX_TWEENS_PER_FRAME, totalFloors / 10); // 分散到10帧左右

				int processed = 0;
				float startDelay = 0f;

				for (int i = startIdx; i <= endIdx && i < count; i += step)
				{
					scrFloor target = floors[i];
					Transform targetTransform = target.transform;
					var moveTweens = target.moveTweens;

					Vector3 targetPos = target.startPos + posOffset;
					float finalRotZ = (target.startRot + new Vector3(0, 0, rotZ)).z;

					// 创建Tween请求
					var request = new MoveFloorRequest(
						targetTransform,
						instance.positionUsed ? targetPos.x : targetTransform.position.x,
						instance.positionUsed ? targetPos.y : targetTransform.position.y,
						instance.rotationUsed ? finalRotZ : targetTransform.eulerAngles.z,
						instance.scaleUsed ? scaleTarget.x : targetTransform.localScale.x,
						instance.scaleUsed ? scaleTarget.y : targetTransform.localScale.y,
						instance.duration,
						instance.ease,
						instance.positionUsed,
						instance.rotationUsed,
						instance.scaleUsed
					);

					// 分帧处理：延迟启动Tween
					TweenBatchQueue.Enqueue(request);

					processed++;
					if (processed % floorsPerFrame == 0)
					{
						startDelay += FRAME_SPREAD;
					}
				}

				Main.Logger?.Log($"[ExtremeOpt] Processed {processed} MoveFloor events in batches");
			}
		}

		#endregion

		#region MoveDecoration Extreme Optimization

		/// <summary>
		/// 极端优化版的MoveDecoration - 检测到大量事件时分帧处理
		/// </summary>
		[HarmonyPatch(typeof(ffxMoveDecorationsPlus), nameof(ffxMoveDecorationsPlus.StartEffect))]
		public static class ExtremeMoveDecorPatch
		{
			public static bool Prefix(ffxMoveDecorationsPlus __instance)
			{
				if (!Main.Settings.optimizer.enableOptimizer ||
					!Main.Settings.optimizer.optimizeMoveDecorations)
					return true;

				try
				{
					__instance.AdjustDurationForHardbake();

					// 计算受影响的装饰物数量
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

					// 检测是否是极端情况（大量装饰物）
					bool isExtremeCase = decorCount > BATCH_THRESHOLD;

					if (isExtremeCase)
					{
						// 极端情况：分帧处理
						ProcessExtremeMoveDecor(__instance);
						return false;
					}
					else
					{
						// 普通情况：立即处理
						return true;
					}
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
			
						// 收集所有受影响的装饰物
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
						int decorsPerFrame = Mathf.Min(MAX_TWEENS_PER_FRAME, totalDecors / 10);
			
						int processed = 0;
			
						foreach (var decor in allDecors)
						{
							if (decor == null || decor.transform == null) continue;
			
							// 分帧创建Tween
							EnqueueMoveDecorTween(decor.transform, targetPos, duration, ease);
			
							processed++;
							if (processed % decorsPerFrame == 0)
							{
								// 让下一帧继续处理
								System.Threading.Tasks.Task.Delay(1).Wait();
							}
						}
			
						Main.Logger?.Log($"[ExtremeOpt] Processed {processed} MoveDecor events in batches");
					}
			private static void EnqueueMoveDecorTween(Transform transform, Vector3 targetPos, float duration, Ease ease)
			{
				TweenBatchQueue.Enqueue(new MoveDecorRequest(transform, targetPos, duration, ease));
			}
		}

		private class MoveDecorRequest : TweenRequest
		{
			private readonly Transform _transform;
			private readonly Vector3 _targetPos;
			private readonly float _duration;
			private readonly Ease _ease;

			public MoveDecorRequest(Transform transform, Vector3 targetPos, float duration, Ease ease)
			{
				_transform = transform;
				_targetPos = targetPos;
				_duration = duration;
				_ease = ease;
			}

			public override void Execute()
			{
				try
				{
					if (_transform == null) return;

					_transform.DOMove(_targetPos, _duration).SetEase(_ease);
				}
				catch (Exception e)
				{
					Main.Logger?.Error($"[ExtremeOpt] MoveDecorRequest failed: {e}");
				}
			}
		}

		#endregion

		#region Update Hook - 持续处理待处理的Tween

		/// <summary>
		/// 每帧处理待处理的Tween
		/// </summary>
		[HarmonyPatch(typeof(scnGame), "Update")]
		public static class ProcessPendingTweensPatch
		{
			[HarmonyPostfix]
			public static void Postfix()
			{
				if (!Main.Settings.optimizer.enableOptimizer) return;

				// 持续处理待处理的Tween
				if (TweenBatchQueue.PendingCount > 0)
				{
					TweenBatchQueue.StartProcessing();
				}
			}
		}

		#endregion

		#region Cleanup

		/// <summary>
		/// 清理批处理队列
		/// </summary>
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