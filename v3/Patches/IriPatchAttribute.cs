using Iridium.Config;
using System;
using Iridium.Runtime;

namespace Iridium.Patches
{
	/// <summary>
	/// Declarative patch registration attribute.
	/// <para>
	/// <b>Path</b> defines a hierarchical namespace (e.g. "optimizer/texture", "bugfix/portal").
	/// Parent path segments automatically gate child patches — if a parent is disabled,
	/// all children are disabled regardless of their own conditions.
	/// </para>
	/// <para>
	/// <b>Pre + Condition</b> specify the Settings property that controls this patch.
	/// Pre is the settings group type (e.g. typeof(CompatibilitySettings)),
	/// Condition is the property name (e.g. nameof(...portalTravelFix)).
	/// They are resolved at runtime into a dot-separated path like "compatibility.portalTravelFix".
	/// </para>
	/// <para>
	/// Patches without Pre/Condition inherit their parent path's condition.
	/// Patches with Pre/Condition are AND-gated with the parent: parent must be ON AND own condition must be true.
	/// </para>
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class IriPatchAttribute : Attribute
	{
		/// <summary>
		/// Hierarchical path for this patch (e.g. "optimizer/texture", "bugfix/portal").
		/// </summary>
		public string Path { get; set; } = "";

		/// <summary>
		/// Settings group type. Combined with <see cref="Condition"/> to form
		/// a dot-separated property path (e.g. typeof(CompatibilitySettings) + "portalTravelFix"
		/// → "compatibility.portalTravelFix").
		/// </summary>
		public Type? Pre { get; set; }

		/// <summary>
		/// Optional: multiple settings group types for cross-class compound conditions.
		/// When set, each Condition entry is resolved against its corresponding PreTypes entry.
		/// Example: PreTypes=[typeof(A), typeof(B)], Condition="x,y" → "groupA.x,groupB.y".
		/// </summary>
		public Type[]? PreTypes { get; set; }

		/// <summary>
		/// Property name(s) within the <see cref="Pre"/> settings group.
		/// Comma-separated names are evaluated as AND (all must be true).
		/// When <see cref="PreTypes"/> is set, each entry maps to its corresponding type.
		/// </summary>
		public string? Condition { get; set; }

		/// <summary>
		/// If true, this patch is always active regardless of any conditions.
		/// </summary>
		public bool AlwaysOn { get; set; } = false;

		/// <summary>
		/// Optional: runtime reflection check. If set, the patch is only active
		/// when the specified method exists on the target type.
		/// Format: "TypeName.MethodName" (e.g. "scrController.LockInput").
		/// </summary>
		public string? RequireMethod { get; set; }

		/// <summary>
		/// Runtimes this patch supports. Defaults to Mono only.
		/// </summary>
		public RuntimeKind[] SupportedRuntimes { get; set; } = new[] { RuntimeKind.Mono };
	}
}
