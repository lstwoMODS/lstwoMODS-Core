using System.Numerics;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class InputFloat2Renderer : UIRenderer
{
    private Vector2 _value;
    private string _format;
    private ImGuiInputTextFlags _flags;

    public InputFloat2Renderer(BaseUIElementData data) : base(data)
    {
        var d = (InputFloat2Data)data;
        _value = new Vector2(d.X, d.Y);
        _format = d.Format;
        _flags = (ImGuiInputTextFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (InputFloat2Data)data;
        Data = d;
        Name = d.Name;
        _value = new Vector2(d.X, d.Y);
        _format = d.Format;
        _flags = (ImGuiInputTextFlags)(int)d.Flags;
    }

    public override void Render() { ImGui.InputFloat2(Data.Name, ref _value, _format, _flags); }

    public override BaseUIElementData? GetNewState()
    {
        var d = (InputFloat2Data)Data;
        if (_value.X == d.X && _value.Y == d.Y) return null;
        d.X = _value.X;
        d.Y = _value.Y;
        return new InputFloat2Data { Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled, X = _value.X, Y = _value.Y, Format = _format, Flags = d.Flags };
    }
}
