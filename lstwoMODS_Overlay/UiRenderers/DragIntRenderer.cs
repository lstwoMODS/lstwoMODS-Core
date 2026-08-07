using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class DragIntRenderer : UIRenderer
{
    private int _value;
    private float _speed;
    private int _min, _max;
    private string _format;
    private ImGuiSliderFlags _flags;

    public DragIntRenderer(BaseUIElementData data) : base(data)
    {
        var d = (DragIntData)data;
        _value = d.Value;
        _speed = d.Speed;
        _min = d.Min;
        _max = d.Max;
        _format = d.Format;
        _flags = (ImGuiSliderFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (DragIntData)data;
        Data = d;
        Name = d.Name;
        _value = d.Value;
        _speed = d.Speed;
        _min = d.Min;
        _max = d.Max;
        _format = d.Format;
        _flags = (ImGuiSliderFlags)(int)d.Flags;
    }

    public override void Render() { ImGui.DragInt(Data.Name, ref _value, _speed, _min, _max, _format, _flags); }

    public override BaseUIElementData? GetNewState()
    {
        var d = (DragIntData)Data;
        if (_value == d.Value) return null;
        d.Value = _value;
        return new DragIntData { Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled, Value = _value, Speed = _speed, Min = _min, Max = _max, Format = _format, Flags = d.Flags };
    }
}
