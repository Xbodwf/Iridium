using System;
using System.Collections.Generic;
using System.Reflection;

namespace Iridium.Patches
{
	/// <summary>
	/// A node in the hierarchical path tree.
	/// Pure structure — no condition logic. Conditions are defined
	/// on each patch via <see cref="IriPatchAttribute.Pre"/> + <see cref="IriPatchAttribute.Condition"/>.
	/// </summary>
	public sealed class PatchNode
	{
		public Dictionary<string, PatchNode> Children { get; } = new(StringComparer.OrdinalIgnoreCase);

		public PatchNode() { }

		public PatchNode WithChild(string name, PatchNode child)
		{
			Children[name] = child;
			return this;
		}

		/// <summary>
		/// Find a node by a slash-separated path (e.g. "optimizer/editorFloor/insert").
		/// </summary>
		public static PatchNode? Find(string fullPath)
		{
			if (string.IsNullOrEmpty(fullPath)) return null;

			var segments = fullPath.Split('/');
			PatchNode? current = null;

			for (int i = 0; i < segments.Length; i++)
			{
				if (current == null)
				{
					current = FindRootChild(segments[i]);
				}
				else
				{
					if (!current.Children.TryGetValue(segments[i], out current))
						return null;
				}
			}

			return current;
		}

		/// <summary>
		/// Collect all ancestor path segments from root to this node (inclusive).
		/// Used by PatchManager to resolve inherited conditions.
		/// </summary>
		public static List<string> GetAncestorPaths(string fullPath)
		{
			var result = new List<string>();
			if (string.IsNullOrEmpty(fullPath)) return result;

			var segments = fullPath.Split('/');
			string accumulated = "";
			foreach (var segment in segments)
			{
				accumulated = string.IsNullOrEmpty(accumulated) ? segment : accumulated + "/" + segment;
				result.Add(accumulated);
			}
			return result;
		}

		/// <summary>
		/// Resolve a dot-separated property path (e.g. "optimizer.enableOptimizer")
		/// into a Func&lt;bool&gt; that reads from <see cref="Main.Settings"/>.
		/// Supports negation with "!" prefix (e.g. "!optimizer.dontCompress").
		/// Returns () => true if the path is empty or null.
		/// </summary>
		public static Func<bool> ResolveSetting(string? settingPath)
		{
			if (string.IsNullOrEmpty(settingPath))
				return () => true;

			var segments = settingPath.Split('.');
			if (segments.Length != 2)
			{
				Main.Logger?.Error($"[PatchNode] Invalid setting path: '{settingPath}' (expected 'group.property')");
				return () => false;
			}

			string groupName = segments[0];
			string propertyName = segments[1];

			bool negate = propertyName.StartsWith("!");
			if (negate) propertyName = propertyName.Substring(1);

			var settingsField = typeof(Settings).GetField(groupName,
				BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

			if (settingsField == null)
			{
				Main.Logger?.Error($"[PatchNode] Unknown settings group: '{groupName}'");
				return () => false;
			}

			var field = settingsField.FieldType.GetField(propertyName,
				BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

			if (field == null)
			{
				Main.Logger?.Error($"[PatchNode] Unknown field: '{groupName}.{propertyName}'");
				return () => false;
			}

			// Support bool fields directly, and non-bool fields (e.g. enums) via Convert.ToBoolean
			Type fieldType = field.FieldType;
			Func<object?, bool> converter;
			if (fieldType == typeof(bool))
				converter = obj => (bool)field.GetValue(obj);
			else if (fieldType.IsEnum)
				converter = obj => Convert.ToBoolean(Convert.ToInt32(field.GetValue(obj)));
			else
			{
				Main.Logger?.Error($"[PatchNode] Unsupported field type: '{groupName}.{propertyName}' is {fieldType.Name}");
				return () => false;
			}

			if (negate)
			{
				return () =>
				{
					var settings = Main.Settings;
					if (settings == null) return false;
					var group = settingsField.GetValue(settings);
					if (group == null) return false;
					return !converter(group);
				};
			}

			return () =>
			{
				var settings = Main.Settings;
				if (settings == null) return false;
				var group = settingsField.GetValue(settings);
				if (group == null) return false;
				return converter(group);
			};
		}

		private static PatchNode? FindRootChild(string name)
		{
			var field = typeof(PatchPaths).GetField(name,
				BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
			return field?.GetValue(null) as PatchNode;
		}
	}
}
