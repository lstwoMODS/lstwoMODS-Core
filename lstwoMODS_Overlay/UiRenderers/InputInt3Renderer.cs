using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class InputInt3Renderer : UIRenderer
{
    private int[] _values = new int[3];
    private ImGuiInputTextFlags _flags;

    public InputInt3Renderer(BaseUIElementData data) : base(data)
    {
        var d = (InputInt3Data)data;
        _values[0] = d.X;
        _values[1] = d.Y;
        _values[2] = d.Z;
        _flags = (ImGuiInputTextFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (InputInt3Data)data;
        Data = d;
        Name = d.Name;
        _values[0] = d.X;
        _values[1] = d.Y;
        _values[2] = d.Z;
        _flags = (ImGuiInputTextFlags)(int)d.Flags;
    }

    public override void Render() { ImGui.InputInt3(Data.Name, ref _values[0], _flags); }

    public override BaseUIElementData? GetNewState()
    {
        var d = (InputInt3Data)Data;
        if (_values[0] == d.X && _values[1] == d.Y && _values[2] == d.Z) return null;
        d.X = _values[0];
        d.Y = _values[1];
        d.Z = _values[2];
        return new InputInt3Data { Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled, X = _values[0], Y = _values[1], Z = _values[2], Flags = d.Flags };
    }
}
