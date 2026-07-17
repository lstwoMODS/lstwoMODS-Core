using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class SliderInt4Renderer : UIRenderer
{
    private int[] _values = new int[4];
    private int _min, _max;
    private string _format;
    private ImGuiSliderFlags _flags;

    public SliderInt4Renderer(BaseUIElementData data) : base(data)
    {
        var d = (SliderInt4Data)data;
        _values[0] = d.X;
        _values[1] = d.Y;
        _values[2] = d.Z;
        _values[3] = d.W;
        _min = d.Min;
        _max = d.Max;
        _format = d.Format;
        _flags = (ImGuiSliderFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (SliderInt4Data)data;
        Data = d;
        Name = d.Name;
        _values[0] = d.X;
        _values[1] = d.Y;
        _values[2] = d.Z;
        _values[3] = d.W;
        _min = d.Min;
        _max = d.Max;
        _format = d.Format;
        _flags = (ImGuiSliderFlags)(int)d.Flags;
    }

    public override void Render() { ImGui.SliderInt4(Data.Name, ref _values[0], _min, _max, _format, _flags); }

    public override BaseUIElementData? GetNewState()
    {
        var d = (SliderInt4Data)Data;
        if (_values[0] == d.X && _values[1] == d.Y && _values[2] == d.Z && _values[3] == d.W) return null;
        d.X = _values[0];
        d.Y = _values[1];
        d.Z = _values[2];
        d.W = _values[3];
        return new SliderInt4Data { Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled, X = _values[0], Y = _values[1], Z = _values[2], W = _values[3], Min = _min, Max = _max, Format = _format, Flags = d.Flags };
    }
}
