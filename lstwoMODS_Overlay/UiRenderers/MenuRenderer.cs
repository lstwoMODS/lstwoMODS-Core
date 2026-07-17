using System.Collections.Generic;
using System.Numerics;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class MenuRenderer : UIRenderer
{
    private string _label;
    private bool   _menuEnabled;
    private List<BaseUIElementData> _children;

    public MenuRenderer(BaseUIElementData data) : base(data) { CopyFrom((MenuData)data); }
    private void CopyFrom(MenuData d) { _label = d.Label; _menuEnabled = d.MenuEnabled; _children = d.Children; }
    public override void ApplyState(BaseUIElementData data) { var d=(MenuData)data; var prev=_children; Data=d; Name=d.Name; CopyFrom(d); if (!(d.Children?.Count > 0)) _children=prev; }

    public override void Render()
    {
        if (!ImGui.BeginMenu(_label, _menuEnabled)) return;
        foreach (var child in _children)
            Window.RenderSingleElement(child);
        ImGui.EndMenu();
    }

    public override BaseUIElementData? GetNewState() => null;
}
