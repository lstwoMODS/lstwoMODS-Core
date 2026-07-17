using System.Collections.Generic;
using System.Linq;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

/// <summary>
/// A one-line row whose lead content flows from the left while trailing elements (added via
/// <see cref="WithTrailing"/>) are pinned to the right edge. When the trailing elements don't
/// fit beside the lead they wrap onto their own right-aligned line instead of overflowing
/// a header that stays on one line but degrades gracefully when the container is narrow.
///
/// Lead content follows the same convention as <see cref="Container"/>: pass explicit
/// <see cref="SameLine"/> elements between widgets that should share the line. Trailing
/// elements are automatically laid out side by side.
/// </summary>
public class PinRow : BaseUIElement<PinRow>
{
    public List<BaseUIElement> Children;
    /// <summary>Right-pinned trailing elements (see <see cref="WithTrailing"/>).</summary>
    public List<BaseUIElement> Trailing = new();

    public PinRow(string name, params BaseUIElement[] lead) : base(name)
    {
        Children = new List<BaseUIElement>(lead);
        Data = new PinRowData
        {
            Name     = name,
            Children = Children.Select(c => c.Data).ToList(),
        };
    }

    /// <summary>Set the right-pinned trailing elements (e.g. action buttons). Chainable.</summary>
    public PinRow WithTrailing(params BaseUIElement[] elements)
    {
        Trailing = new List<BaseUIElement>(elements);
        ((PinRowData)Data).LineChildren = Trailing.Select(e => e.Data).ToList();
        return this;
    }

    public override IEnumerable<BaseUIElement> GetChildren() => Children.Concat(Trailing);
}
