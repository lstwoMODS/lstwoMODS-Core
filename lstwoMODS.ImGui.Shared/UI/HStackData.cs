using System.Collections.Generic;

namespace lstwoMODS.ImGui.Shared.UI
{
    public enum HStackWidthMode
    {
        /// <summary>Fills the entire remaining content width (default). Children are sized to fill proportional slots.</summary>
        Full,
        /// <summary>Uses CalcItemWidth() as total width  the same value buttons use with UseContentWidth. Respects any active PushItemWidth from a parent.</summary>
        Content,
        /// <summary>Fixed pixel width set via <see cref="HStackData.ExplicitWidth"/>. Children fill proportional slots within that width.</summary>
        Explicit,
    }

    /// <summary>
    /// Renders children side by side on a single line, dividing a total width
    /// proportionally between slots.
    /// </summary>
    public class HStackData : BaseUIElementData
    {
        public List<BaseUIElementData> Children { get; set; } = new List<BaseUIElementData>();

        /// <summary>
        /// Per-slot proportions. Empty = equal widths.
        /// Slots without a matching entry get proportion 1.
        /// </summary>
        public List<float> Proportions { get; set; } = new List<float>();

        /// <summary>
        /// Horizontal spacing between slots in pixels.
        /// -1 (default) = use ImGui ItemSpacing.X.
        /// </summary>
        public float Spacing { get; set; } = -1f;

        /// <summary>How the total width of the HStack is determined.</summary>
        public HStackWidthMode WidthMode { get; set; } = HStackWidthMode.Full;

        /// <summary>Used when <see cref="WidthMode"/> is <see cref="HStackWidthMode.Explicit"/>.</summary>
        public float ExplicitWidth { get; set; } = 0f;
    }
}
