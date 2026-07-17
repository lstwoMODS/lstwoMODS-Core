using System;

namespace lstwoMODS_Core.Hacks
{
    /// <summary>
    /// Mark a field or property to expose it as a UI setting and register it in <see cref="ModRegistry"/>.
    /// The auto-builder maps the value type to an appropriate widget:
    ///   bool → Checkbox | float → DragFloat (optionally clamped) or SliderFloat (if both min+max set)
    ///   int → DragInt or SliderInt | string → InputText | Color → ColorEdit4
    ///   Vector2/3/4 → DragFloat2/3/4 | Enum → Combo
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class ModSettingAttribute : Attribute
    {
        /// <summary>Display label. Defaults to the member name.</summary>
        public string Label       { get; set; }
        /// <summary>Minimum clamp for numeric types. float.NaN = unset. Both Min+Max → SliderXxx; one or neither → DragXxx.</summary>
        public float  Min         { get; set; } = float.NaN;
        /// <summary>Maximum clamp for numeric types. float.NaN = unset. Both Min+Max → SliderXxx; one or neither → DragXxx.</summary>
        public float  Max         { get; set; } = float.NaN;
        /// <summary>Printf format string for numeric types (e.g. "%.2f", "%d").</summary>
        public string Format      { get; set; } = null;
        /// <summary>Tooltip text shown on hover.</summary>
        public string Description { get; set; } = null;
        /// <summary>Include in the auto-built UI panel. false = registered in ModRegistry only.</summary>
        public bool   ShowInUI    { get; set; } = true;
        /// <summary>Sort order within the auto-built panel. Lower = higher up.</summary>
        public int    Order       { get; set; } = 0;
        /// <summary>ImGui ID pushed before this widget to avoid label-based ID conflicts.</summary>
        public string Id          { get; set; } = null;
        /// <summary>When true, draws a horizontal separator line above this setting in the auto-built panel.</summary>
        public bool   Separator      { get; set; } = false;
        /// <summary>When non-null, draws a SeparatorText with this label above this setting in the auto-built panel.</summary>
        public string SeparatorText  { get; set; } = null;
        /// <summary>Which widget the auto-builder should use. Defaults to <see cref="WidgetType.Default"/> (auto-select by type).</summary>
        public WidgetType Widget { get; set; } = WidgetType.Default;
        /// <summary>The Drag Speed when the Widget is of type Drag.</summary>
        public float Speed { get; set; } = 0.05f;
        /// <summary>
        /// When true, the auto-builder wraps the input widget in an HStack with a button on the right.
        /// The widget's label is hidden (rendered as <c>##{MemberName}</c>) and value changes are deferred
        /// until the button is clicked, at which point the pending value is committed via <c>SetValue</c>.
        /// Ref&lt;T&gt; bindings are bypassed in this mode so the Ref's value isn't updated on every keystroke.
        /// </summary>
        public bool   ApplyButton      { get; set; } = false;
        /// <summary>Custom label for the apply button. Null defaults to <c>"Set {Label}"</c>.</summary>
        public string ApplyButtonLabel { get; set; } = null;
        /// <summary>
        /// When true (default), the auto-built UI adds a right-click context menu on this setting
        /// offering "Add to macro" and "Create hotkey". Set false to suppress it for this setting.
        /// </summary>
        public bool   Macroable        { get; set; } = true;

        public ModSettingAttribute() { }
        public ModSettingAttribute(string label) { Label = label; }
        public ModSettingAttribute(string label, float min, float max) { Label = label; Min = min; Max = max; }
    }
}
