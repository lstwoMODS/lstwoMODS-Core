using System.Collections.Generic;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class MainMenuBarRenderer : UIRenderer
{
    private List<BaseUIElementData> _children;

    public MainMenuBarRenderer(BaseUIElementData data) : base(data) { _children = ((MainMenuBarData)data).Children; }
    public override void ApplyState(BaseUIElementData data) { var d = (MainMenuBarData)data; Data = d; Name = d.Name; if (d.Children?.Count > 0) _children = d.Children; }

    public override void Render()
    {
        if (!ImGui.BeginMainMenuBar()) return;
        foreach (var child in _children)
            Window.RenderSingleElement(child);
        ImGui.EndMainMenuBar();
    }

    public override BaseUIElementData? GetNewState() => null;
}
