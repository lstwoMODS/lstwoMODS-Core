using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class InputInt4Renderer : UIRenderer
{
    private int[] _values = new int[4];
    private ImGuiInputTextFlags _flags;

    public InputInt4Renderer(BaseUIElementData data) : base(data)
    {
        var d = (InputInt4Data)data;
        _values[0] = d.X;
        _values[1] = d.Y;
        _values[2] = d.Z;
        _values[3] = d.W;
        _flags = (ImGuiInputTextFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (InputInt4Data)data;
        Data = d;
        Name = d.Name;
        _values[0] = d.X;
        _values[1] = d.Y;
        _values[2] = d.Z;
        _values[3] = d.W;
        _flags = (ImGuiInputTextFlags)(int)d.Flags;
    }

    public override void Render() { ImGui.InputInt4(Data.Name, ref _values[0], _flags); }

    public override BaseUIElementData? GetNewState()
    {
        var d = (InputInt4Data)Data;
        if (_values[0] == d.X && _values[1] == d.Y && _values[2] == d.Z && _values[3] == d.W) return null;
        d.X = _values[0];
        d.Y = _values[1];
        d.Z = _values[2];
        d.W = _values[3];
        return new InputInt4Data { Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled, X = _values[0], Y = _values[1], Z = _values[2], W = _values[3], Flags = d.Flags };
    }
}
