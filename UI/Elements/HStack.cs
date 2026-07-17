using System.Collections.Generic;
using System.Linq;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

/// <summary>
/// Renders children side by side on a single line, dividing a total width
/// proportionally between slots. Input widgets and buttons inside each slot
/// are automatically sized to fill it.
///
/// <example><code>
/// // Equal thirds, full content width (default)
/// new HStack("row", sliderA, sliderB, sliderC)
///
/// // 1 : 2 : 1 split
/// new HStack("row", labelEl, inputEl, btnEl).WithProportions(1f, 2f, 1f)
///
/// // Respect parent PushItemWidth instead of filling full width
/// new HStack("row", a, b).WithContentWidth()
///
/// // Fixed 300 px total
/// new HStack("row", a, b).WithWidth(300f)
///
/// // Custom slot spacing
/// new HStack("row", a, b).WithSpacing(8f)
/// </code></example>
/// </summary>
public class HStack : BaseUIElement<HStack>
{
    public List<BaseUIElement> Children { get; }

    public HStack(string name, params BaseUIElement[] children) : base(name)
    {
        Children = new List<BaseUIElement>(children);

        Data = new HStackData
        {
            Name     = name,
            Children = Children.Select(c => c.Data).ToList()
        };
    }

    /// <summary>
    /// Override the per-slot proportions. Pass one value per child; slots without
    /// a matching proportion get proportion 1. The values are relative, not percentages.
    /// </summary>
    public HStack WithProportions(params float[] proportions)
    {
        ((HStackData)Data).Proportions = new List<float>(proportions);
        return this;
    }

    /// <summary>
    /// Override the horizontal spacing between slots.
    /// Positive = pixels. -1 (default) = use ImGui ItemSpacing.X.
    /// </summary>
    public HStack WithSpacing(float spacing)
    {
        ((HStackData)Data).Spacing = spacing;
        return this;
    }

    /// <summary>
    /// Switch to content-width mode: children render at their natural sizes side by side,
    /// no slot widths are forced. Buttons auto-size to their label; inputs use their default width.
    /// </summary>
    public HStack WithContentWidth()
    {
        ((HStackData)Data).WidthMode = HStackWidthMode.Content;
        return this;
    }

    /// <summary>
    /// Size the HStack to an explicit pixel width.
    /// </summary>
    public HStack WithWidth(float width)
    {
        var d = (HStackData)Data;
        d.WidthMode    = HStackWidthMode.Explicit;
        d.ExplicitWidth = width;
        return this;
    }

    public override IEnumerable<BaseUIElement> GetChildren() => Children;
}
