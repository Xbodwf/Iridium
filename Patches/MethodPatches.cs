using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ADOFAI;
using DG.Tweening;
using DG.Tweening.Core;
using HarmonyLib;
using Iridium.Core;
using UnityEngine;

namespace Iridium.Patches
{
    /// <summary>
    /// ForceDifficultyUIPatch - 使用 PatchMethod 系统的 StdPatchMethod<br/>
    /// scrMisc 是静态类，所以 T=object
    /// 替换原有的 MiscPatches.ForceDifficultyUIPatch
    /// </summary>
    public class StdForceDifficultyUIPatch : StdPatchMethod<object, DifficultyUIMode>
    {
        public StdForceDifficultyUIPatch() : base(PatchTypes.Postfix) { }

        public override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(scrMisc), "DetermineDifficultyUIMode");
        }

        public override bool Method(object[] args)
        {
            if (ADOBase.isCLSLevel) result = DifficultyUIMode.ShowAll;
            return true;
        }
    }

    /// <summary>
    /// HideBetaWatermarkPatch - 使用 PatchMethod 系统的 StdPatchMethod<br/>
    /// 替换原有的 MiscPatches.HideBetaWatermarkPatch
    /// </summary>
    public class StdHideBetaWatermarkPatch : StdPatchMethod<scrEnableIfBeta, object>
    {
        public StdHideBetaWatermarkPatch() : base(PatchTypes.Postfix) { }

        public override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(scrEnableIfBeta), "Awake");
        }

        public override bool Method(object[] args)
        {
            instance?.gameObject.SetActive(false);
            return true;
        }
    }

    // ============================================================
    //  HitSoundPatch — scnGame.Update Prefix
    //  同步 HitSound 音高与 Conductor pitch
    // ============================================================
    public class StdHitSoundPatch : StdPatchMethod<scnGame, object>
    {
        private static Transform? _audioSourceContainer;
        private static readonly ConditionalWeakTable<Transform, AudioSource> _audioSourceCache = new();
        private static float _lastPitch = 1f;

        public StdHitSoundPatch() : base(PatchTypes.Prefix) { }

        public override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(scnGame), "Update");

        public override bool Method(object[] args)
        {
            float targetPitch = ADOBase.conductor?.song?.pitch ?? 1f;
            float playbackSpeed = ADOBase.editor?.playbackSpeed ?? 1f;
            float finalPitch = targetPitch * playbackSpeed;

            if (Mathf.Approximately(finalPitch, _lastPitch)) return true;
            _lastPitch = finalPitch;

            if (_audioSourceContainer == null)
            {
                var go = GameObject.Find("AudioSource Container");
                if (go == null) return true;
                _audioSourceContainer = go.transform;
            }

            int childCount = _audioSourceContainer.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = _audioSourceContainer.GetChild(i);
                if (child.name != "Audio Source(Clone)") continue;

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
            return true;
        }
    }

    // ============================================================
    //  AutoplayTextPositionPatch — scrUIController.Update Prefix
    //  移动 autoplay 文本到指定位置
    // ============================================================
    public class StdAutoplayTextPositionPatch : StdPatchMethod<scrUIController, object>
    {
        public StdAutoplayTextPositionPatch() : base(PatchTypes.Prefix) { }

        public override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(scrUIController), "Update");

        public override bool Method(object[] args)
        {
            MiscPatches.RefreshAutoplayTextPosition();
            return true;
        }
    }

    // ============================================================
    //  CustomBpmPatch — scrConductor.Update Prefix
    //  在大厅场景中替换 BPM 为自定义值
    // ============================================================
    public class StdCustomBpmPatch : StdPatchMethod<scrConductor, object>
    {
        public StdCustomBpmPatch() : base(PatchTypes.Prefix) { }

        public override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(scrConductor), "Update");

        public override bool Method(object[] args)
        {
            if (scrConductor.instance is null) return true;
            if (!MiscPatches.LobbyScenes.Contains(ADOBase.sceneName)) return true;
            scrConductor.instance.bpm = Main.Settings.lobbyMusic.customBpm;
            return true;
        }
    }

    // ============================================================
    //  ProcessPendingTweensPatch — scnGame.Update Postfix
    //  极端优化：每帧处理分帧队列中的 Tween
    // ============================================================
    public class StdProcessPendingTweensPatch : StdPatchMethod<scnGame, object>
    {
        public StdProcessPendingTweensPatch() : base(PatchTypes.Postfix) { }

        public override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(scnGame), "Update");

        public override bool Method(object[] args)
        {
            if (ExtremeOptimizationPatches.TweenBatchQueue.PendingCount > 0)
                ExtremeOptimizationPatches.TweenBatchQueue.StartProcessing();
            return true;
        }
    }

    // ============================================================
    //  FilterPlusPatch — ffxSetFilterPlus.StartEffect Prefix
    // ============================================================
    public class StdFilterPlusPatch : StdPatchMethod<ffxSetFilterPlus, object>
    {
        public StdFilterPlusPatch() : base(PatchTypes.Prefix) { }

        public override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(ffxSetFilterPlus), "StartEffect");

        public override bool Method(object[] args)
            => true; // 始终允许原始方法执行（占位）
    }

    // ============================================================
    //  FilterAdvancedPlusPatch — ffxSetFilterAdvancedPlus.StartEffect Prefix
    // ============================================================
    public class StdFilterAdvancedPlusPatch : StdPatchMethod<ffxSetFilterAdvancedPlus, object>
    {
        public StdFilterAdvancedPlusPatch() : base(PatchTypes.Prefix) { }

        public override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(ffxSetFilterAdvancedPlus), "StartEffect");

        public override bool Method(object[] args)
        {
            if (instance != null)
            {
                instance.AdjustDurationForHardbake();
                // 保持与原始一致的黑名单检查（空块）
                if (ffxSetFilterAdvancedPlus.blacklistedFilterKeywords.Any(k => instance.filterName.Contains(k))) { }
            }
            return true;
        }
    }

    // ============================================================
    //  HitTextMeshShowPatch — scrHitTextMesh.Show Prefix
    //  在偏移量模式下替换判定文本为 ms 数值
    // ============================================================
    public class StdHitTextMeshShowPatch : StdPatchMethod<scrHitTextMesh, object>
    {
        private static readonly AccessTools.FieldRef<scrHitTextMesh, TextMesh> _getText =
            AccessTools.FieldRefAccess<scrHitTextMesh, TextMesh>("text");

        public StdHitTextMeshShowPatch() : base(PatchTypes.Prefix) { }

        public override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(scrHitTextMesh), "Show");

        public override bool Method(object[] args)
        {
            if (!Main.Settings.judgeText.showAsOffset) return true;

            float angle = (float)args[0];
            var textMesh = _getText(instance);
            if (textMesh != null)
            {
                double timing = CalculateTimingFromAngle(angle);
                textMesh.text = GetOffsetText(timing);
            }
            return true;
        }

        /// <summary>
        /// 从角度偏移量计算实际 timing (ms)。
        /// angularOffset = targetExitAngle - actualAngle
        /// 符号约定：提前(Early) = +, 错过(Late) = -
        /// </summary>
        private static double CalculateTimingFromAngle(float angularOffset)
        {
            var controller = scrController.instance;
            var conductor = scrConductor.instance;
            if (controller == null || conductor == null) return 0;

            double bpm = conductor.bpm;
            double speed = controller.speed;
            double pitch = conductor.song.pitch;

            double standardTiming = angularOffset * (controller.isCW ? 1.0 : -1.0) * 60000.0 / (Math.PI * bpm * speed * pitch);
            return -standardTiming;
        }

        private static string GetOffsetText(double timing)
        {
            if (double.IsNaN(timing) || double.IsInfinity(timing)) return "0ms";
            long ms = (long)Math.Round(timing);
            return $"{(ms >= 0 ? "+" : "-")}{Math.Abs(ms)}ms";
        }
    }

    // ============================================================
    //  HitTextMeshInitPatch — scrHitTextMesh.Init Postfix
    //  替换判定文本为自定义文本（非偏移量模式）
    // ============================================================
    public class StdHitTextMeshInitPatch : StdPatchMethod<scrHitTextMesh, object>
    {
        private static readonly AccessTools.FieldRef<scrHitTextMesh, TextMesh> _getText =
            AccessTools.FieldRefAccess<scrHitTextMesh, TextMesh>("text");

        public StdHitTextMeshInitPatch() : base(PatchTypes.Postfix) { }

        public override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(scrHitTextMesh), "Init", new[] { typeof(HitMargin) });

        public override bool Method(object[] args)
        {
            if (Main.Settings.judgeText.showAsOffset) return true;

            HitMargin hitMargin = (HitMargin)args[0];
            var textMesh = _getText(instance);
            if (textMesh != null)
                textMesh.text = Main.Settings.judgeText.GetTextForHitMargin((int)hitMargin);
            return true;
        }
    }

    // ============================================================
    //  MoveFloorPatch — ffxMoveFloorPlus.StartEffect Prefix
    //  优化 Move Track：缓存 Transform 引用、跳过无变化的属性
    // ============================================================
    public class StdMoveFloorPatch : StdPatchMethod<ffxMoveFloorPlus, object>
    {
        public StdMoveFloorPatch() : base(PatchTypes.Prefix) { }

        public override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(ffxMoveFloorPlus), "StartEffect");

        public override bool Method(object[] args)
        {
            var inst = instance;
            if (inst == null) return true;

            inst.AdjustDurationForHardbake();

            int startIdx = inst.start;
            int endIdx = inst.end;
            if (endIdx < startIdx)
            {
                (startIdx, endIdx) = (endIdx, startIdx);
            }

            Vector3 posOffset = new Vector3(inst.targetPos.x, inst.targetPos.y, 0f);
            float rotZ = inst.targetRot;
            Vector3 scaleTarget = new Vector3(inst.targetScaleV2.x, inst.targetScaleV2.y, 1f);

            var floors = ADOBase.lm.listFloors;
            int count = floors.Count;
            int step = 1 + inst.gapLength;

            for (int i = startIdx; i <= endIdx && i < count; i += step)
            {
                scrFloor target = floors[i];
                Transform targetTransform = TrackOptimizationPatches.GetTransform(target);
                var moveTweens = target.moveTweens;

                if (inst.positionUsed)
                {
                    Vector3 targetPos = target.startPos + posOffset;
                    if (!float.IsNaN(targetPos.x))
                    {
                        if (moveTweens.TryGetValue(TweenType.PositionX, out var oldT)) oldT.Kill(true);
                        if (!Mathf.Approximately(targetTransform.position.x, targetPos.x))
                            moveTweens[TweenType.PositionX] = DOTween.To(
                                (DOGetter<float>)(() => targetTransform.position.x),
                                (DOSetter<float>)(x => targetTransform.MoveX(x)),
                                targetPos.x, inst.duration).SetEase(inst.ease);
                    }
                    if (!float.IsNaN(targetPos.y))
                    {
                        if (moveTweens.TryGetValue(TweenType.PositionY, out var oldT)) oldT.Kill(true);
                        if (!Mathf.Approximately(targetTransform.position.y, targetPos.y))
                            moveTweens[TweenType.PositionY] = DOTween.To(
                                (DOGetter<float>)(() => targetTransform.position.y),
                                (DOSetter<float>)(y => targetTransform.MoveY(y)),
                                targetPos.y, inst.duration).SetEase(inst.ease);
                    }
                }

                if (inst.rotationUsed)
                {
                    if (moveTweens.TryGetValue(TweenType.Rotation, out var oldT)) oldT.Kill(true);
                    float finalRotZ = (target.startRot + new Vector3(0, 0, rotZ)).z;
                    if (!Mathf.Approximately(targetTransform.eulerAngles.z, finalRotZ))
                    {
                        moveTweens[TweenType.Rotation] = DOTween.To(
                            (DOGetter<float>)(() => target.tweenRot.z),
                            (DOSetter<float>)(r =>
                            {
                                target.tweenRot.z = r;
                                targetTransform.eulerAngles = target.tweenRot;
                            }),
                            finalRotZ, inst.duration).SetEase(inst.ease);
                    }
                }

                if (inst.scaleUsed)
                {
                    if (!float.IsNaN(scaleTarget.x))
                    {
                        if (moveTweens.TryGetValue(TweenType.ScaleX, out var oldT)) oldT.Kill(true);
                        moveTweens[TweenType.ScaleX] = targetTransform.DOScaleX(scaleTarget.x, inst.duration).SetEase(inst.ease);
                    }
                    if (!float.IsNaN(scaleTarget.y))
                    {
                        if (moveTweens.TryGetValue(TweenType.ScaleY, out var oldT)) oldT.Kill(true);
                        moveTweens[TweenType.ScaleY] = targetTransform.DOScaleY(scaleTarget.y, inst.duration).SetEase(inst.ease);
                    }
                }

                if (inst.opacityUsed)
                {
                    if (moveTweens.TryGetValue(TweenType.Opacity, out var oldT)) oldT.Kill(true);
                    if (!Mathf.Approximately(target.opacity, inst.targetOpacity))
                    {
                        var t = target.TweenOpacity(inst.targetOpacity, inst.duration, inst.ease);
                        if (t != null) moveTweens[TweenType.Opacity] = t;
                    }
                }
            }
            return false;
        }
    }

    // ============================================================
    //  RecolorFloorPatch — ffxRecolorFloorPlus.StartEffect Prefix
    //  优化 Recolor Track：批量处理 floor 颜色/发光动画
    // ============================================================
    public class StdRecolorFloorPatch : StdPatchMethod<ffxRecolorFloorPlus, object>
    {
        public StdRecolorFloorPatch() : base(PatchTypes.Prefix) { }

        public override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(ffxRecolorFloorPlus), "StartEffect");

        public override bool Method(object[] args)
        {
            var inst = instance;
            if (inst == null) return true;

            inst.AdjustDurationForHardbake();

            int startIdx = inst.start;
            int endIdx = inst.end;
            if (endIdx < startIdx)
            {
                (startIdx, endIdx) = (endIdx, startIdx);
            }

            var floors = ADOBase.lm.listFloors;
            int count = floors.Count;
            int step = 1 + inst.gapLength;
            float pitch = inst.cond.song.pitch;

            for (int i = startIdx; i <= endIdx && i < count; i += step)
            {
                scrFloor target = floors[i];
                target.styleNum = (int)inst.style;
                target.UpdateAngle(false);
                target.SetTrackStyle(inst.style);

                var mt = target.moveTweens;
                if (mt.TryGetValue(TweenType.Color, out var t1)) t1.Kill(true);
                if (mt.TryGetValue(TweenType.Glow, out var t2)) t2.Kill(true);

                target.ColorFloor(inst.colorType, inst.color1, inst.color2,
                    inst.colorAnimDuration / pitch, inst.pulseType,
                    (float)inst.pulseLength, inst.start, inst.duration, inst.ease);

                mt[TweenType.Glow] = DOTween.To(
                    (DOGetter<float>)(() => target.glowMultiplier),
                    (DOSetter<float>)(x => target.glowMultiplier = x),
                    inst.glowMult, inst.duration).SetEase(inst.ease);
            }

            inst.enabled = false;
            return false;
        }
    }

    // ============================================================
    //  DecorationManagerLateUpdatePatch — scrDecorationManager.LateUpdate Prefix
    //  只更新有活动 Tween 或视差效果的装饰物
    // ============================================================
    public class StdDecorationManagerLateUpdatePatch : StdPatchMethod<scrDecorationManager, object>
    {
        public StdDecorationManagerLateUpdatePatch() : base(PatchTypes.Prefix) { }

        public override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(scrDecorationManager), "LateUpdate");

        public override bool Method(object[] args)
        {
            var allDecorations = instance?.allDecorations;
            if (allDecorations == null) return true;

            int count = allDecorations.Count;
            for (int i = 0; i < count; i++)
            {
                scrDecoration dec = allDecorations[i];
                if (FfxOptimizationPatches.ShouldUpdate(dec))
                    dec.UpdatePosition();
            }
            return false;
        }
    }
}
