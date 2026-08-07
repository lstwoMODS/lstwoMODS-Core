using System.Collections.Generic;
using System.Numerics;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class MenuBarRenderer : UIRenderer
{
    private List<BaseUIElementData> _children;

    public MenuBarRenderer(BaseUIElementData data) : base(data) { _children = ((MenuBarData)data).Children; }
    public override void ApplyState(BaseUIElementData data) { var d=(MenuBarData)data; Data=d; Name=d.Name; if (d.Children?.Count > 0) _children=d.Children; }

    public override void Render()
    {
        if (!ImGui.BeginMenuBar()) return;
        foreach (var child in _children)
            Window.RenderSingleElement(child);
        ImGui.EndMenuBar();
    }

    public override BaseUIElementData? GetNewState() => null;
}
