using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace Iridium.Patches
{
    [HarmonyPatch(typeof(scnGame), "Update")]
    public static class HitSoundPatch
    {
        private static Transform? _audioSourceContainer;
        private static readonly ConditionalWeakTable<Transform, AudioSource> _audioSourceCache = new();
        private static float _lastPitch = 1f;

        [HarmonyPrefix]
        public static void Prefix()
        {
            UpdateHitSoundPitch();
        }

        public static void UpdateHitSoundPitch()
        {
            float targetPitch = ADOBase.conductor?.song?.pitch ?? 1f;
            float playbackSpeed = ADOBase.editor?.playbackSpeed ?? 1f;
            float finalPitch = targetPitch * playbackSpeed;

            // 只在 pitch 变化时更新
            if (Mathf.Approximately(finalPitch, _lastPitch)) return;
            _lastPitch = finalPitch;

            // 缓存 container 引用
            if (_audioSourceContainer == null)
            {
                var go = GameObject.Find("AudioSource Container");
                if (go == null) return;
                _audioSourceContainer = go.transform;
            }

            // 遍历子对象
            int childCount = _audioSourceContainer.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = _audioSourceContainer.GetChild(i);
                if (child.name != "Audio Source(Clone)") continue;

                // 使用缓存获取 AudioSource
                if (!_audioSourceCache.TryGetValue(child, out var audioSource) || audioSource == null)
                {
                    audioSource = child.GetComponent<AudioSource>();
                    if (audioSource != null)
                    {
                        _audioSourceCache.Remove(child);
                        _audioSourceCache.Add(child, audioSource);
                    }
                }

                if (audioSource != null)
                {
                    audioSource.pitch = finalPitch;
                }
            }
        }
    }
}