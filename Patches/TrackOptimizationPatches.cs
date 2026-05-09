using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using ADOFAI;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;

namespace Iridium.Patches
{
    public static class TrackOptimizationPatches
    {
        internal static ConditionalWeakTable<scrFloor, Transform> _floorTransformCache = new ConditionalWeakTable<scrFloor, Transform>();

        internal static Transform GetTransform(scrFloor floor)
        {
            if (!_floorTransformCache.TryGetValue(floor, out var t))
            {
                t = floor.transform;
                _floorTransformCache.Add(floor, t);
            }
            return t;
        }

        // NOTE: MoveFloorStartEffectPatch and RecolorFloorStartEffectPatch
        // moved to StdPatchMethod classes in MethodPatches.cs

        [HarmonyPatch(typeof(scnGame), "Awake")]
        public static class CleanupPatch
        {
            public static void Postfix()
            {
                _floorTransformCache = new ConditionalWeakTable<scrFloor, Transform>();
            }
        }
    }
}