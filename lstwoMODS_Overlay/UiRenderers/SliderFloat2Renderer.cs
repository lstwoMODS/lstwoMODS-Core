using System.Numerics;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class SliderFloat2Renderer : UIRenderer
{
    private Vector2 _value;
    private float _min, _max;
    private string _format;
    private ImGuiSliderFlags _flags;

    public SliderFloat2Renderer(BaseUIElementData data) : base(data)
    {
        var d = (SliderFloat2Data)data;
        _value = new Vector2(d.X, d.Y);
        _min = d.Min;
        _max = d.Max;
        _format = d.Format;
        _flags = (ImGuiSliderFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (SliderFloat2Data)data;
        Data = d;
        Name = d.Name;
        _value = new Vector2(d.X, d.Y);
        _min = d.Min;
        _max = d.Max;
        _format = d.Format;
        _flags = (ImGuiSliderFlags)(int)d.Flags;
    }

    public override void Render() { ImGui.SliderFloat2(Data.Name, ref _value, _min, _max, _format, _flags); }

    public override BaseUIElementData? GetNewState()
    {
        var d = (SliderFloat2Data)Data;
        if (_value.X == d.X && _value.Y == d.Y) return null;
        d.X = _value.X;
        d.Y = _value.Y;
        return new SliderFloat2Data { Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled, X = _value.X, Y = _value.Y, Min = _min, Max = _max, Format = _format, Flags = d.Flags };
    }
}
