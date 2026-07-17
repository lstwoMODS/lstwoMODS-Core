using System.Numerics;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class ColorEdit4Renderer : UIRenderer
{
    private Vector4 _color;
    private ImGuiColorEditFlags _flags;

    public ColorEdit4Renderer(BaseUIElementData data) : base(data)
    {
        var d = (ColorEdit4Data)data;
        _color = new Vector4(d.R, d.G, d.B, d.A);
        _flags = (ImGuiColorEditFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (ColorEdit4Data)data;
        Data = d;
        Name = d.Name;
        _color = new Vector4(d.R, d.G, d.B, d.A);
        _flags = (ImGuiColorEditFlags)(int)d.Flags;
    }

    public override void Render() { ImGui.ColorEdit4(Data.Name, ref _color, _flags); }

    public override BaseUIElementData? GetNewState()
    {
        var d = (ColorEdit4Data)Data;
        if (_color.X == d.R && _color.Y == d.G && _color.Z == d.B && _color.W == d.A) return null;
        d.R = _color.X;
        d.G = _color.Y;
        d.B = _color.Z;
        d.A = _color.W;
        return new ColorEdit4Data { Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled, R = _color.X, G = _color.Y, B = _color.Z, A = _color.W, Flags = d.Flags };
    }
}
