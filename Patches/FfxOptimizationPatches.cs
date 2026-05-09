using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using DG.Tweening;

namespace Iridium.Patches
{
    /// <summary>
    /// 优化 ffx 脚本的性能 - 减少装饰物更新频率
    /// 核心策略：只更新有活动Tween或视差效果的装饰物
    /// </summary>
    public static class FfxOptimizationPatches
    {
        // 需要持续更新的装饰物（有活动Tween或视差效果）
        private static readonly HashSet<scrDecoration> _activeDecorations = new();
        private static readonly object _activeLock = new();

        /// <summary>
        /// 标记装饰物为活动（需要持续更新）
        /// </summary>
        public static void MarkActive(scrDecoration decoration)
        {
            if (decoration == null) return;
            lock (_activeLock)
            {
                _activeDecorations.Add(decoration);
            }
        }

        /// <summary>
        /// 移除活动标记
        /// </summary>
        public static void UnmarkActive(scrDecoration decoration)
        {
            if (decoration == null) return;
            lock (_activeLock)
            {
                _activeDecorations.Remove(decoration);
            }
        }

        /// <summary>
        /// 检查装饰物是否需要更新
        /// </summary>
        internal static bool ShouldUpdate(scrDecoration dec)
        {
            if (dec == null || !dec.GetVisible()) return false;

            // 检查是否有活动的Tween
            if (dec.eventTweens != null && dec.eventTweens.Count > 0)
            {
                foreach (var tween in dec.eventTweens.Values)
                {
                    if (tween != null && tween.IsActive() && tween.IsPlaying())
                    {
                        return true; // 有活动Tween，需要更新
                    }
                }
            }

            // 检查是否有视差效果（需要跟随相机）
            if (dec.parallax != null && (dec.parallax.multiplier.x != 1f || dec.parallax.multiplier.y != 1f))
            {
                return true; // 有视差效果，需要更新
            }

            return false; // 静止装饰物，不需要更新
        }
    }
}
