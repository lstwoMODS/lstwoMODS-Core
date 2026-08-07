using System.Collections.Generic;

namespace lstwoMODS.ImGui.Shared.UI
{
    /// <summary>
    /// Responsive grid container. The renderer computes how many columns fit the available
    /// width (each at least <see cref="MinCellWidth"/>), stretches the cells so every row
    /// fills the width edge to edge, and wraps children into rows. Growing the window widens
    /// the cells until one more column fits, then they snap smaller again. ChildWindow
    /// children automatically adopt the cell width.
    /// </summary>
    public class FlowGridData : BaseUIElementData
    {
        /// <summary>Minimum cell width  controls when another column fits.</summary>
        public float MinCellWidth { get; set; } = 200f;
        /// <summary>Optional cap on cell width (0 = stretch without limit).</summary>
        public float MaxCellWidth { get; set; }
        public List<BaseUIElementData> Children { get; set; } = new List<BaseUIElementData>();
        /// <summary>Optional single "tail" element (index 0) the renderer stretches to fill the
        /// trailing empty space of the last partial row  e.g. a drop zone that maps to "after the
        /// last cell". Named <c>LineChildren</c> so it rides the framework's stripped-child-list
        /// plumbing (shallow updates, element-tree walks) without extra registration.</summary>
        public List<BaseUIElementData> LineChildren { get; set; } = new List<BaseUIElementData>();
    }
}
