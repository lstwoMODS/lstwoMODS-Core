using System;

namespace lstwoMODS_Core.Hacks
{
    /// <summary>
    /// Annotate a [ModAction] method parameter to control how the auto-builder renders it.
    /// <code>
    /// [ModAction("Apply Force")]
    /// void ApplyForce([ModActionParam(WidgetType.Slider, Min = 0, Max = 100)] float magnitude,
    ///                 [ModActionParam(Widget = WidgetType.Input)] Vector3 direction) { }
    /// </code>
    /// <see cref="Widget"/> can be passed positionally or set by name, whichever reads better.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class ModActionParamAttribute : Attribute
    {
        /// <summary>The label that gets displayed on the option for this param</summary>
        public string Label { get; set; } = null;
        /// <summary>Which widget the auto-builder should use. Defaults to <see cref="WidgetType.Default"/> (auto-select by type).</summary>
        public WidgetType Widget { get; set; }

        /// <summary>Drag speed for <see cref="WidgetType.Drag"/> widgets. 0 = default (1.0).</summary>
        public float  Speed  { get; set; } = 0f;
        /// <summary>Minimum value for <see cref="WidgetType.Slider"/>. float.NaN = unset → falls back to Drag.</summary>
        public float  Min    { get; set; } = float.NaN;
        /// <summary>Maximum value for <see cref="WidgetType.Slider"/>.</summary>
        public float  Max    { get; set; } = float.NaN;
        /// <summary>Printf format string (e.g. "%.2f", "%d"). Null = type default.</summary>
        public string Format { get; set; } = null;
        /// <summary>ImGui ID pushed before this widget to avoid label-based ID conflicts.</summary>
        public string Id     { get; set; } = null;

        public ModActionParamAttribute(WidgetType widget = WidgetType.Default) { Widget = widget; }
    }

    /// <summary>Widget variants available for auto-built action parameter UI.</summary>
    public enum WidgetType
    {
        /// <summary>Auto-select based on value type: float/int/vector → Drag, string → Input, Color → Color4.</summary>
        Default,
        /// <summary>DragFloat / DragInt / DragFloat2/3/4.</summary>
        Drag,
        /// <summary>SliderFloat / SliderInt. Requires Min and Max; falls back to Drag if unset.</summary>
        Slider,
        /// <summary>InputFloat / InputInt / InputFloat2/3/4 / InputText.</summary>
        Input,
        /// <summary>ColorEdit3, for Color/Col or Vector3/Vec3.</summary>
        Color3,
        /// <summary>ColorEdit4, for Color/Col or Vector4/Vec4.</summary>
        Color4,
    }
}
