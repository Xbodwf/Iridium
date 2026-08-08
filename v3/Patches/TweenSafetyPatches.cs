using System.Collections;
using System.Reflection;
using DG.Tweening;
using HarmonyLib;

namespace Iridium.Patches
{
	/// <summary>
	/// DOTween 回收安全补丁：
	/// 当 DOTween.defaultRecyclable = true 时，已完成的 Tween 会回到对象池复用。
	/// ADOFAI 多处存储/遍历 Tween 引用时未检查 IsActive()，导致对已回收的 Tween 进行操作，
	/// 造成动画异常、ease 错乱、特效损坏等问题。
	/// 
	/// 本文件中包含的所有补丁都通过安全的 IsActive() + null 检查来防止上述问题。
	/// </summary>
	public static class TweenSafetyPatches
	{
		/// <summary>
		/// scrVfxPlus.Reset() 会清理 filterTween，但未清理 pausedTweens。
		/// 导致过期的 Tween 引用跨关卡累积，在下一次恢复(Play)时对已回收的 Tween 进行操作。
		/// </summary>
		[HarmonyPatch(typeof(scrVfxPlus), "Reset")]
		public static class ClearPausedTweensOnReset
		{
			[HarmonyPostfix]
			public static void Postfix(scrVfxPlus __instance)
			{
				if (__instance.pausedTweens is { Count: > 0 })
					__instance.pausedTweens.Clear();
			}
		}

		/// <summary>
		/// scrDecoration.OnDestroy() 遍历 eventTweens 直接调用 Kill(false)，
		/// 未对可能已回收/无效的 Tween 做安全检查。
		/// </summary>
		[HarmonyPatch(typeof(scrDecoration), "OnDestroy")]
		public static class SafeDecorationOnDestroy
		{
			[HarmonyPrefix]
			public static bool Prefix(scrDecoration __instance)
			{
				if (__instance.eventTweens != null)
				{
					foreach (var tween in __instance.eventTweens.Values)
					{
						if (tween != null && tween.IsActive())
							tween.Kill(false);
					}
				}

				ffxSetFilterAdvancedPlus.CleanVariables(__instance.gameObject);
				return false;
			}
		}

		/// <summary>
		/// ffxPlusBase.Kill() 遍历 eventTweens 直接调用 Kill(false)，
		/// 没有 null 检查也没有 IsActive() 检查。
		/// eventTweens 是 protected 属性，通过 AccessTools 反射读取。
		/// </summary>
		[HarmonyPatch(typeof(ffxPlusBase), "Kill")]
		public static class SafeFfxPlusBaseKill
		{
			private static readonly PropertyInfo EventTweensProp =
				AccessTools.Property(typeof(ffxPlusBase), "eventTweens");

			[HarmonyPrefix]
			public static bool Prefix(ffxPlusBase __instance)
			{
				var tweens = EventTweensProp?.GetValue(__instance, null) as IEnumerable;
				if (tweens != null)
				{
					foreach (Tween t in tweens)
					{
						if (t != null && t.IsActive())
							t.Kill(false);
					}
				}

				return false;
			}
		}

		/// <summary>
		/// ffxPlusBase.ScrubToTime() 中的两个问题：
		/// 1. Goto() / Kill() 操作前没有 IsActive() 检查
		/// 2. 未检查 tween 活跃性就直接添加到 pausedTweens
		/// 
		/// 该补丁完整替换原方法并添加了安全性检查。
		/// </summary>
		[HarmonyPatch(typeof(ffxPlusBase), "ScrubToTime")]
		public static class SafeFfxPlusBaseScrubToTime
		{
			private static readonly PropertyInfo EventTweensProp =
				AccessTools.Property(typeof(ffxPlusBase), "eventTweens");

			[HarmonyPrefix]
			public static bool Prefix(ffxPlusBase __instance, float t)
			{
				if ((double)t < __instance.startTime)
					return false;

				__instance.StartEffect();
				__instance.triggered = true;

				double endTime = __instance.startTime + __instance.duration;
				bool isAtEnd = UnityEngine.Mathf.Approximately(t, (float)endTime);

				if (ADOBase.currentLevel == "XO-X" && __instance is ffxCameraPlus)
					isAtEnd = true;

				if ((double)t >= endTime || isAtEnd)
				{
					// 超出持续时间：Kill 所有活跃的 Tween
					var tweens = EventTweensProp?.GetValue(__instance, null) as IEnumerable;
					if (tweens != null)
					{
						foreach (Tween tw in tweens)
						{
							if (tw != null && tw.IsActive())
								tw.Kill(true);
						}
					}

					return false;
				}

				// 在持续时间内：Goto 到指定时间 + 加入暂停列表
				float elapsed = (float)((double)t - __instance.startTime);
				var allTweens = EventTweensProp?.GetValue(__instance, null) as IEnumerable;
				if (allTweens != null)
				{
					foreach (Tween tw in allTweens)
					{
						bool isAtZero = UnityEngine.Mathf.Approximately(elapsed, 0f);
						if (tw != null && tw.IsActive())
						{
							tw.Goto(elapsed, true);
						}

						// 只在 tween 活跃时加入暂停列表，防止 stale 引用累积
						if (!isAtZero && tw != null && tw.IsActive())
						{
							if (__instance.vfx is { pausedTweens: not null })
								__instance.vfx.pausedTweens.Add(tw);
						}
					}
				}

				return false;
			}
		}
	}
}
