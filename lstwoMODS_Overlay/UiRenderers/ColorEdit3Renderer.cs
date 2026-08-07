using System.Numerics;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class ColorEdit3Renderer : UIRenderer
{
    private Vector3 _color;
    private ImGuiColorEditFlags _flags;

    public ColorEdit3Renderer(BaseUIElementData data) : base(data)
    {
        var d = (ColorEdit3Data)data;
        _color = new Vector3(d.R, d.G, d.B);
        _flags = (ImGuiColorEditFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (ColorEdit3Data)data;
        Data = d;
        Name = d.Name;
        _color = new Vector3(d.R, d.G, d.B);
        _flags = (ImGuiColorEditFlags)(int)d.Flags;
    }

    public override void Render() { ImGui.ColorEdit3(Data.Name, ref _color, _flags); }

    public override BaseUIElementData? GetNewState()
    {
        var d = (ColorEdit3Data)Data;
        if (_color.X == d.R && _color.Y == d.G && _color.Z == d.B) return null;
        d.R = _color.X;
        d.G = _color.Y;
        d.B = _color.Z;
        return new ColorEdit3Data { Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled, R = _color.X, G = _color.Y, B = _color.Z, Flags = d.Flags };
    }
}
