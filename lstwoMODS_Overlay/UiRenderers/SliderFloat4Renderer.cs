using System.Numerics;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class SliderFloat4Renderer : UIRenderer
{
    private Vector4 _value;
    private float _min, _max;
    private string _format;
    private ImGuiSliderFlags _flags;

    public SliderFloat4Renderer(BaseUIElementData data) : base(data)
    {
        var d = (SliderFloat4Data)data;
        _value = new Vector4(d.X, d.Y, d.Z, d.W);
        _min = d.Min;
        _max = d.Max;
        _format = d.Format;
        _flags = (ImGuiSliderFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (SliderFloat4Data)data;
        Data = d;
        Name = d.Name;
        _value = new Vector4(d.X, d.Y, d.Z, d.W);
        _min = d.Min;
        _max = d.Max;
        _format = d.Format;
        _flags = (ImGuiSliderFlags)(int)d.Flags;
    }

    public override void Render() { ImGui.SliderFloat4(Data.Name, ref _value, _min, _max, _format, _flags); }

    public override BaseUIElementData? GetNewState()
    {
        var d = (SliderFloat4Data)Data;
        if (_value.X == d.X && _value.Y == d.Y && _value.Z == d.Z && _value.W == d.W) return null;
        d.X = _value.X;
        d.Y = _value.Y;
        d.Z = _value.Z;
        d.W = _value.W;
        return new SliderFloat4Data { Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled, X = _value.X, Y = _value.Y, Z = _value.Z, W = _value.W, Min = _min, Max = _max, Format = _format, Flags = d.Flags };
    }
}
