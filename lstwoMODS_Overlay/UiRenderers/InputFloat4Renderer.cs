using System.Numerics;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class InputFloat4Renderer : UIRenderer
{
    private Vector4 _value;
    private string _format;
    private ImGuiInputTextFlags _flags;

    public InputFloat4Renderer(BaseUIElementData data) : base(data)
    {
        var d = (InputFloat4Data)data;
        _value = new Vector4(d.X, d.Y, d.Z, d.W);
        _format = d.Format;
        _flags = (ImGuiInputTextFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (InputFloat4Data)data;
        Data = d;
        Name = d.Name;
        _value = new Vector4(d.X, d.Y, d.Z, d.W);
        _format = d.Format;
        _flags = (ImGuiInputTextFlags)(int)d.Flags;
    }

    public override void Render() { ImGui.InputFloat4(Data.Name, ref _value, _format, _flags); }

    public override BaseUIElementData? GetNewState()
    {
        var d = (InputFloat4Data)Data;
        if (_value.X == d.X && _value.Y == d.Y && _value.Z == d.Z && _value.W == d.W) return null;
        d.X = _value.X;
        d.Y = _value.Y;
        d.Z = _value.Z;
        d.W = _value.W;
        return new InputFloat4Data { Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled, X = _value.X, Y = _value.Y, Z = _value.Z, W = _value.W, Format = _format, Flags = d.Flags };
    }
}
