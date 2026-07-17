using System.Numerics;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class SelectableRenderer : UIRenderer
{
    private bool                 _selected;
    private ImGuiSelectableFlags _flags;
    private float _sizeX, _sizeY;

    public SelectableRenderer(BaseUIElementData data) : base(data)
    {
        var d = (SelectableData)data;
        _selected = d.Selected;
        _flags    = (ImGuiSelectableFlags)(int)d.Flags;
        _sizeX    = d.SizeX; _sizeY = d.SizeY;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (SelectableData)data;
        Data = d; Name = d.Name;
        _selected = d.Selected;
        _flags    = (ImGuiSelectableFlags)(int)d.Flags;
        _sizeX    = d.SizeX; _sizeY = d.SizeY;
    }

    public override void Render()
    {
        ImGui.Selectable(Data.Name, ref _selected, _flags, new Vector2(_sizeX, _sizeY));
    }

    public override BaseUIElementData? GetNewState()
    {
        var d = (SelectableData)Data;
        if (_selected == d.Selected) return null;
        d.Selected = _selected;
        return new SelectableData { Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled, Selected = _selected, Flags = d.Flags, SizeX = _sizeX, SizeY = _sizeY };
    }
}
