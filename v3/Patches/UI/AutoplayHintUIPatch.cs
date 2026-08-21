using Iridium.Config;
using System.Reflection;
using ADOFAI;
using HarmonyLib;
using Iridium;
using Iridium.UI;
using UnityEngine;

namespace Iridium.Patches.UI
{
	[HarmonyPatch(typeof(scnEditor), "Update")]
	[IriPatch(Path = "ui/autoplayHint", AlwaysOn = true)]
	public static class AutoplayHintUIPatch
	{
		private static FieldInfo _controlsTipField;

		[HarmonyPrepare]
		public static void Prepare()
		{
			_controlsTipField = typeof(scnEditor).GetField("controlsTip");
		}

		[HarmonyPostfix]
		public static void Postfix(scnEditor __instance)
		{
			if (_controlsTipField == null) return;
			var controlsTip = _controlsTipField.GetValue(__instance) as UnityEngine.UI.Text;
			if (controlsTip == null) return;
			if (!RDC.auto || !__instance.playMode) return;
			if (!Main.Settings.ui.showAutoplayHintUI)
			{
				controlsTip.text = "";
			}
			else if (Main.Settings.ui.customAutoplayHint)
			{
				controlsTip.text = string.IsNullOrEmpty(Main.Settings.ui.autoplayHintTemplate)
					? ""
					: ProcessAutoplayHintTemplate(Main.Settings.ui.autoplayHintTemplate, __instance.pausedInPlayMode);
			}
		}

		private static string ProcessAutoplayHintTemplate(string template, bool paused)
		{
			var autoplayText = RDString.Get("status.autoplay");
			var statusText = paused ? RDString.Get("status.paused") : Localization.Get("AutoplayStatusEnabled");
			var keyCode = Main.Settings.compatibility.editorPauseAllowed && Main.Settings.compatibility.editorPauseEnabled
				? Main.Settings.compatibility.editorPauseKey
				: 32;
			var keyName = GetKeyDisplayName(keyCode);
			return template
				.Replace("{autoplay}", autoplayText)
				.Replace("{status}", statusText)
				.Replace("{key}", keyName);
		}

		private static string GetKeyDisplayName(int keyCode)
		{
			switch ((KeyCode)keyCode)
			{
				case KeyCode.Space: return "Space";
				case KeyCode.Return: return "Enter";
				case KeyCode.Escape: return "Esc";
				case KeyCode.Tab: return "Tab";
				case KeyCode.LeftShift: return "L-Shift";
				case KeyCode.RightShift: return "R-Shift";
				case KeyCode.LeftControl: return "L-Ctrl";
				case KeyCode.RightControl: return "R-Ctrl";
				case KeyCode.LeftAlt: return "L-Alt";
				case KeyCode.RightAlt: return "R-Alt";
				default:
					if (keyCode >= (int)KeyCode.A && keyCode <= (int)KeyCode.Z)
						return ((char)('A' + (keyCode - (int)KeyCode.A))).ToString();
					if (keyCode >= (int)KeyCode.Alpha0 && keyCode <= (int)KeyCode.Alpha9)
						return ((char)('0' + (keyCode - (int)KeyCode.Alpha0))).ToString();
					if (keyCode >= (int)KeyCode.F1 && keyCode <= (int)KeyCode.F12)
						return "F" + (keyCode - (int)KeyCode.F1 + 1);
					return ((KeyCode)keyCode).ToString();
			}
		}
	}
}
