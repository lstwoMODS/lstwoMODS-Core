using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class SliderFloatRenderer : UIRenderer
{
    private float _value, _min, _max;
    private string _format;
    private ImGuiSliderFlags _flags;

    public SliderFloatRenderer(BaseUIElementData data) : base(data)
    {
        var d = (SliderFloatData)data;
        _value = d.Value;
        _min = d.Min;
        _max = d.Max;
        _format = d.Format;
        _flags = (ImGuiSliderFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (SliderFloatData)data;
        Data = d;
        Name = d.Name;
        _value = d.Value;
        _min = d.Min;
        _max = d.Max;
        _format = d.Format;
        _flags = (ImGuiSliderFlags)(int)d.Flags;
    }

    public override void Render() { ImGui.SliderFloat(Data.Name, ref _value, _min, _max, _format, _flags); }

    public override BaseUIElementData? GetNewState()
    {
        var d = (SliderFloatData)Data;
        if (_value == d.Value) return null;
        d.Value = _value;
        return new SliderFloatData { Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled, Value = _value, Min = _min, Max = _max, Format = _format, Flags = d.Flags };
    }
}
