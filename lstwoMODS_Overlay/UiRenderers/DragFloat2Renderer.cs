using System.Numerics;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class DragFloat2Renderer : UIRenderer
{
    private Vector2 _value;
    private float _speed, _min, _max;
    private string _format;
    private ImGuiSliderFlags _flags;

    public DragFloat2Renderer(BaseUIElementData data) : base(data)
    {
        var d = (DragFloat2Data)data;
        _value = new Vector2(d.X, d.Y);
        _speed = d.Speed;
        _min = d.Min;
        _max = d.Max;
        _format = d.Format;
        _flags = (ImGuiSliderFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (DragFloat2Data)data;
        Data = d;
        Name = d.Name;
        _value = new Vector2(d.X, d.Y);
        _speed = d.Speed;
        _min = d.Min;
        _max = d.Max;
        _format = d.Format;
        _flags = (ImGuiSliderFlags)(int)d.Flags;
    }

    public override void Render() { ImGui.DragFloat2(Data.Name, ref _value, _speed, _min, _max, _format, _flags); }

    public override BaseUIElementData? GetNewState()
    {
        var d = (DragFloat2Data)Data;
        if (_value.X == d.X && _value.Y == d.Y) return null;
        d.X = _value.X;
        d.Y = _value.Y;
        return new DragFloat2Data { Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled, X = _value.X, Y = _value.Y, Speed = _speed, Min = _min, Max = _max, Format = _format, Flags = d.Flags };
    }
}
