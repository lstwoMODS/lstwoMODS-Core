using System.Collections.Generic;
using System.Linq;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

/// <summary>
/// Responsive grid container: fits as many columns as the available width allows (each at
/// least <c>minCellWidth</c> wide) and stretches the cells so every row fills the width edge
/// to edge  growing the window widens the cells until one more column fits, then they snap
/// smaller again. ChildWindow children automatically adopt the cell width.
/// </summary>
public class FlowGrid : BaseUIElement<FlowGrid>
{
    public List<BaseUIElement> Children;

    /// <summary>Optional element the renderer stretches to fill the trailing empty space of the
    /// last partial row (see <see cref="WithTail"/>).</summary>
    public BaseUIElement Tail;

    /// <param name="minCellWidth">Minimum cell width  controls when another column fits.</param>
    /// <param name="maxCellWidth">Optional cap on cell width (0 = stretch without limit).</param>
    public FlowGrid(string name, float minCellWidth, float maxCellWidth = 0f, params BaseUIElement[] children) : base(name)
    {
        Children = new List<BaseUIElement>(children);
        Data = new FlowGridData
        {
            Name         = name,
            MinCellWidth = minCellWidth,
            MaxCellWidth = maxCellWidth,
            Children     = Children.Select(c => c.Data).ToList(),
        };
    }

    /// <summary>Set a tail element that fills the trailing empty space of the last partial row
    /// (e.g. a drop zone mapping to "after the last cell"). Not a grid cell; runtime child
    /// inserts still only touch the cells. Chainable.</summary>
    public FlowGrid WithTail(BaseUIElement tail)
    {
        Tail = tail;
        ((FlowGridData)Data).LineChildren = tail == null
            ? new List<BaseUIElementData>()
            : new List<BaseUIElementData> { tail.Data };
        return this;
    }

    public override IEnumerable<BaseUIElement> GetChildren()
        => Tail == null ? Children : Children.Concat(new[] { Tail });
}
