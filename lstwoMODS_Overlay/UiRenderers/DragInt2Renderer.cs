using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class DragInt2Renderer : UIRenderer
{
    private int[] _values = new int[2];
    private float _speed;
    private int _min, _max;
    private string _format;
    private ImGuiSliderFlags _flags;

    public DragInt2Renderer(BaseUIElementData data) : base(data)
    {
        var d = (DragInt2Data)data;
        _values[0] = d.X;
        _values[1] = d.Y;
        _speed = d.Speed;
        _min = d.Min;
        _max = d.Max;
        _format = d.Format;
        _flags = (ImGuiSliderFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (DragInt2Data)data;
        Data = d;
        Name = d.Name;
        _values[0] = d.X;
        _values[1] = d.Y;
        _speed = d.Speed;
        _min = d.Min;
        _max = d.Max;
        _format = d.Format;
        _flags = (ImGuiSliderFlags)(int)d.Flags;
    }

    public override void Render() { ImGui.DragInt2(Data.Name, ref _values[0], _min, _max, _format, _flags); }

    public override BaseUIElementData? GetNewState()
    {
        var d = (DragInt2Data)Data;
        if (_values[0] == d.X && _values[1] == d.Y) return null;
        d.X = _values[0];
        d.Y = _values[1];
        return new DragInt2Data { Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled, X = _values[0], Y = _values[1], Speed = _speed, Min = _min, Max = _max, Format = _format, Flags = d.Flags };
    }
}
