using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class SliderAngleRenderer : UIRenderer
{
    private float _angleRad, _minDeg, _maxDeg;
    private string _format;
    private ImGuiSliderFlags _flags;

    public SliderAngleRenderer(BaseUIElementData data) : base(data)
    {
        var d = (SliderAngleData)data;
        _angleRad = d.AngleRad;
        _minDeg = d.MinDegrees;
        _maxDeg = d.MaxDegrees;
        _format = d.Format;
        _flags = (ImGuiSliderFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (SliderAngleData)data;
        Data = d;
        Name = d.Name;
        _angleRad = d.AngleRad;
        _minDeg = d.MinDegrees;
        _maxDeg = d.MaxDegrees;
        _format = d.Format;
        _flags = (ImGuiSliderFlags)(int)d.Flags;
    }

    public override void Render() { ImGui.SliderAngle(Data.Name, ref _angleRad, _minDeg, _maxDeg, _format, _flags); }

    public override BaseUIElementData? GetNewState()
    {
        var d = (SliderAngleData)Data;
        if (_angleRad == d.AngleRad) return null;
        d.AngleRad = _angleRad;
        return new SliderAngleData { Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled, AngleRad = _angleRad, MinDegrees = _minDeg, MaxDegrees = _maxDeg, Format = _format, Flags = d.Flags };
    }
}
