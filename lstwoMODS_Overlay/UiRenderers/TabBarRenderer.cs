using System.Collections.Generic;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class TabBarRenderer : UIRenderer
{
    private ImGuiTabBarFlags _flags;
    private List<BaseUIElementData> _children;

    public TabBarRenderer(BaseUIElementData data) : base(data)
    {
        var d = (TabBarData)data;
        _flags = (ImGuiTabBarFlags)(int)d.Flags; _children = d.Children;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (TabBarData)data; Data = d; Name = d.Name;
        _flags = (ImGuiTabBarFlags)(int)d.Flags; if (d.Children?.Count > 0) _children = d.Children;
    }

    public override void Render()
    {
        if (ImGui.BeginTabBar(Data.Name, _flags))
        {
            foreach (var child in _children)
                Window.RenderSingleElement(child);
            ImGui.EndTabBar();
        }
    }

    public override BaseUIElementData? GetNewState() => null;
}
