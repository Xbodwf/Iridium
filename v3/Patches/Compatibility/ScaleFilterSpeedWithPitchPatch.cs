using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ADOFAI;
using HarmonyLib;
using UnityEngine;
using Iridium.Config;

namespace Iridium.Patches.Compatibility
{
	/// <summary>
	/// 缩放CameraFilterPack着色器内部动画速度以适应音高变化
	/// 所有CameraFilterPack组件的OnRenderImage中都有 TimeX += Time.deltaTime 驱动内部动画，
	/// 该patch将其替换为 TimeX += Time.deltaTime * pitch，
	/// 使录制降pitch视频后期加速后滤镜着色器动画速度与原始一致
	/// === 与旧方案的区别 ===
	/// 旧方案错误地只修正了tween的duration (通过adjustDurationForHardbake)，
	/// 但滤镜着色器内部的TimeX仍然使用未缩放的Time.deltaTime，
	/// 导致录制降pitch后着色器动画以原速运行，后期加速后显得更快
	/// 新方案直接修改着色器的时间推进速度，从根源解决
	/// </summary>
	[HarmonyPatch]
	[IriPatch(Path = "compatibility/scaleFilter", Pre = typeof(CompatibilitySettings), Condition = "scaleFilterSpeedWithPitch")]
	public static class ScaleFilterSpeedWithPitchPatch
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			try { Assembly.Load("Assembly-CSharp-firstpass"); }
			catch { }

			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				Type[] types;
				try { types = asm.GetTypes(); }
				catch { continue; }

				foreach (var type in types)
				{
					if (!type.Name.Contains("CameraFilterPack")) continue;
					var method = type.GetMethod("OnRenderImage",
						BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
					if (method != null) yield return method;
				}
			}
		}

		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var get_deltaTime = AccessTools.PropertyGetter(typeof(Time), "deltaTime");
			var getMultiplier = AccessTools.Method(typeof(ScaleFilterSpeedWithPitchPatch), nameof(GetPitchMultiplier));

			foreach (var code in instructions)
			{
				yield return code;
				if (code.Calls(get_deltaTime))
				{
					yield return new CodeInstruction(OpCodes.Call, getMultiplier);
					yield return new CodeInstruction(OpCodes.Mul);
				}
			}
		}

		static float GetPitchMultiplier()
		{
			if (!Main.Settings.compatibility.scaleFilterSpeedWithPitch) return 1f;
			var cond = scrConductor.instance;
			if (cond == null || cond.song == null) return 1f;
			return cond.song.pitch;
		}
	}
}
