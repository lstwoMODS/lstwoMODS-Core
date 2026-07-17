using System.Collections.Generic;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class TabItemRenderer : UIRenderer
{
    private string _label;
    private bool _open;
    private bool _showClose;
    private ImGuiTabItemFlags _flags;
    private List<BaseUIElementData> _children;

    public TabItemRenderer(BaseUIElementData data) : base(data)
    {
        var d = (TabItemData)data;
        _label = d.Label; _open = d.Open; _showClose = d.ShowClose; _flags = (ImGuiTabItemFlags)(int)d.Flags; _children = d.Children;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (TabItemData)data; Data = d; Name = d.Name;
        _label = d.Label; _open = d.Open; _showClose = d.ShowClose; _flags = (ImGuiTabItemFlags)(int)d.Flags; if (d.Children?.Count > 0) _children = d.Children;
    }

    public override void Render()
    {
        bool show;
        if (_showClose)
            show = ImGui.BeginTabItem(_label, ref _open, _flags);
        else
            show = ImGui.BeginTabItem(_label, _flags);

        if (show)
        {
            foreach (var child in _children)
                Window.RenderSingleElement(child);
            ImGui.EndTabItem();
        }
    }

    public override BaseUIElementData? GetNewState()
    {
        var d = (TabItemData)Data;
        if (_open == d.Open) return null;
        d.Open = _open;
        return new TabItemData { Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled, Label = _label, Open = _open, ShowClose = _showClose, Flags = d.Flags, Children = _children };
    }
}
