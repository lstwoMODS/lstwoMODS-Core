using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class DragInt3Renderer : UIRenderer
{
    private int[] _values = new int[3];
    private float _speed;
    private int _min, _max;
    private string _format;
    private ImGuiSliderFlags _flags;

    public DragInt3Renderer(BaseUIElementData data) : base(data)
    {
        var d = (DragInt3Data)data;
        _values[0] = d.X;
        _values[1] = d.Y;
        _values[2] = d.Z;
        _speed = d.Speed;
        _min = d.Min;
        _max = d.Max;
        _format = d.Format;
        _flags = (ImGuiSliderFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (DragInt3Data)data;
        Data = d;
        Name = d.Name;
        _values[0] = d.X;
        _values[1] = d.Y;
        _values[2] = d.Z;
        _speed = d.Speed;
        _min = d.Min;
        _max = d.Max;
        _format = d.Format;
        _flags = (ImGuiSliderFlags)(int)d.Flags;
    }

    public override void Render() { ImGui.DragInt3(Data.Name, ref _values[0], _min, _max, _format, _flags); }

    public override BaseUIElementData? GetNewState()
    {
        var d = (DragInt3Data)Data;
        if (_values[0] == d.X && _values[1] == d.Y && _values[2] == d.Z) return null;
        d.X = _values[0];
        d.Y = _values[1];
        d.Z = _values[2];
        return new DragInt3Data { Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled, X = _values[0], Y = _values[1], Z = _values[2], Speed = _speed, Min = _min, Max = _max, Format = _format, Flags = d.Flags };
    }
}
