using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class DragFloatRenderer : UIRenderer
{
    private float _value;
    private float _speed;
    private float _min;
    private float _max;
    private string _format;
    private ImGuiSliderFlags _flags;

    public DragFloatRenderer(BaseUIElementData data) : base(data)
    {
        var d = (DragFloatData)data;
        _value = d.Value;
        _speed = d.Speed;
        _min = d.Min;
        _max = d.Max;
        _format = d.Format;
        _flags = (ImGuiSliderFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (DragFloatData)data;
        Data = d;
        _value = d.Value;
        _speed = d.Speed;
        _min = d.Min;
        _max = d.Max;
        _format = d.Format;
        _flags = (ImGuiSliderFlags)(int)d.Flags;
    }

    public override void Render()
    {
        ImGui.DragFloat(Data.Name, ref _value, _speed, _min, _max, _format, _flags);
    }

    public override BaseUIElementData? GetNewState()
    {
        var d = (DragFloatData)Data;
        if (_value == d.Value)
            return null;

        d.Value = _value;

        return new DragFloatData
        {
            Id = Data.Id,
            Name = Data.Name,
            Enabled = Data.Enabled,
            Value = _value,
            Speed = _speed,
            Min = _min,
            Max = _max,
            Format = _format,
            Flags = d.Flags
        };
    }
}
