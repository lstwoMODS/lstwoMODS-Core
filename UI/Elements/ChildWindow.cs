using System.Collections.Generic;
using System.Linq;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class ChildWindow : BaseUIElement<ChildWindow>
{
    public List<BaseUIElement> Children;

    /// <param name="sizeX">Width. 0 = fill remaining width.</param>
    /// <param name="sizeY">Height. 0 = fills remaining height.</param>
    public ChildWindow(string name, float sizeX = 0f, float sizeY = 0f, params BaseUIElement[] children) : base(name)
    {
        Children = new List<BaseUIElement>(children);
        Data = new ChildWindowData
        {
            Name     = name,
            SizeX    = sizeX,
            SizeY    = sizeY,
            Children = Children.Select(c => c.Data).ToList()
        };
    }

    public ChildWindow WithFlags(ImGuiChildFlags childFlags) { ((ChildWindowData)Data).ChildFlags = childFlags; return this; }

    /// <summary>Fill the remaining height minus room for <paramref name="lines"/> widget rows
    /// below the child, so following siblings (button rows etc.) pin to the parent's bottom.
    /// Overrides sizeY. Chainable.</summary>
    public ChildWindow WithFooterReserve(float lines = 1f) { ((ChildWindowData)Data).ReserveFooterLines = lines; return this; }
    public ChildWindow WithWindowFlags(ImGuiWindowFlags windowFlags) { ((ChildWindowData)Data).WindowFlags = windowFlags; return this; }

    public override IEnumerable<BaseUIElement> GetChildren() => Children;

    /// <summary>Scroll content to the bottom on next render frame.</summary>
    public void ScrollToBottom() { ((ChildWindowData)Data).ScrollHereY = 1.0f; MarkChanged(); }
    /// <summary>Scroll content to the top on next render frame.</summary>
    public void ScrollToTop()    { ((ChildWindowData)Data).ScrollHereY = 0.0f; MarkChanged(); }
    /// <summary>Scroll to a relative position (0=top, 0.5=centre, 1=bottom).</summary>
    public void ScrollTo(float ratio) { ((ChildWindowData)Data).ScrollHereY = ratio; MarkChanged(); }
    /// <summary>Set absolute vertical scroll in pixels.</summary>
    public void SetScrollY(float px)  { ((ChildWindowData)Data).ScrollToY = px; MarkChanged(); }
}
