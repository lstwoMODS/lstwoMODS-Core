using System.Numerics;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class InputFloat3Renderer : UIRenderer
{
    private Vector3 _value;
    private string _format;
    private ImGuiInputTextFlags _flags;

    public InputFloat3Renderer(BaseUIElementData data) : base(data)
    {
        var d = (InputFloat3Data)data;
        _value = new Vector3(d.X, d.Y, d.Z);
        _format = d.Format;
        _flags = (ImGuiInputTextFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (InputFloat3Data)data;
        Data = d;
        Name = d.Name;
        _value = new Vector3(d.X, d.Y, d.Z);
        _format = d.Format;
        _flags = (ImGuiInputTextFlags)(int)d.Flags;
    }

    public override void Render() { ImGui.InputFloat3(Data.Name, ref _value, _format, _flags); }

    public override BaseUIElementData? GetNewState()
    {
        var d = (InputFloat3Data)Data;
        if (_value.X == d.X && _value.Y == d.Y && _value.Z == d.Z) return null;
        d.X = _value.X;
        d.Y = _value.Y;
        d.Z = _value.Z;
        return new InputFloat3Data { Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled, X = _value.X, Y = _value.Y, Z = _value.Z, Format = _format, Flags = d.Flags };
    }
}
