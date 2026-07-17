using System;

namespace lstwoMODS_Core.Hacks
{
    /// <summary>
    /// Mark a parameterless method to expose it as a button in the auto-built UI panel
    /// and register it in <see cref="ModRegistry"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ModActionAttribute : Attribute
    {
        /// <summary>Button label. Defaults to the method name.</summary>
        public string Label       { get; set; }
        /// <summary>Tooltip text shown on hover.</summary>
        public string Description { get; set; } = null;
        /// <summary>Include in the auto-built UI panel. false = registered in ModRegistry only.</summary>
        public bool   ShowInUI    { get; set; } = true;
        /// <summary>Sort order within the auto-built panel.</summary>
        public int    Order       { get; set; } = 0;

        /// <summary>ImGui ID pushed before this widget to avoid label-based ID conflicts.</summary>
        public string Id             { get; set; } = null;
        /// <summary>When true, draws a horizontal separator line above this action in the auto-built panel.</summary>
        public bool   Separator      { get; set; } = false;
        /// <summary>When non-null, draws a SeparatorText with this label above this action in the auto-built panel.</summary>
        public string SeparatorText  { get; set; } = null;
        /// <summary>Make the button width match the input content width (ImGui.CalcItemWidth). Default true.</summary>
        public bool   ContentWidth   { get; set; } = true;
        /// <summary>
        /// When true (default), the auto-built UI adds a right-click context menu on this action
        /// offering "Add to macro" and "Create hotkey". Set false to suppress it for this action.
        /// </summary>
        public bool   Macroable      { get; set; } = true;

        /// <summary>
        /// Parameter names to exclude from the auto-built UI.
        /// Excluded parameters are still passed to the method using their default value.
        /// </summary>
        public string[] ExcludeParameters { get; set; } = null;

        public ModActionAttribute() { }
        public ModActionAttribute(string label) { Label = label; }
    }
}
