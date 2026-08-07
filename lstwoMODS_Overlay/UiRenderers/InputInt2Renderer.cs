using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class InputInt2Renderer : UIRenderer
{
    private int[] _values = new int[2];
    private ImGuiInputTextFlags _flags;

    public InputInt2Renderer(BaseUIElementData data) : base(data)
    {
        var d = (InputInt2Data)data;
        _values[0] = d.X;
        _values[1] = d.Y;
        _flags = (ImGuiInputTextFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (InputInt2Data)data;
        Data = d;
        Name = d.Name;
        _values[0] = d.X;
        _values[1] = d.Y;
        _flags = (ImGuiInputTextFlags)(int)d.Flags;
    }

    public override void Render() { ImGui.InputInt2(Data.Name, ref _values[0], _flags); }

    public override BaseUIElementData? GetNewState()
    {
        var d = (InputInt2Data)Data;
        if (_values[0] == d.X && _values[1] == d.Y) return null;
        d.X = _values[0];
        d.Y = _values[1];
        return new InputInt2Data { Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled, X = _values[0], Y = _values[1], Flags = d.Flags };
    }
}
