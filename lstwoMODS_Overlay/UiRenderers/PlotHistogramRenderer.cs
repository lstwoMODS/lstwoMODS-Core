using System.Numerics;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class PlotHistogramRenderer : UIRenderer
{
    private float[] _values;
    private int _offset;
    private string _overlayText;
    private float _scaleMin, _scaleMax, _sizeX, _sizeY;

    public PlotHistogramRenderer(BaseUIElementData data) : base(data) { CopyFrom((PlotHistogramData)data); }

    private void CopyFrom(PlotHistogramData d)
    {
        _values = d.Values; _offset = d.Offset; _overlayText = d.OverlayText;
        _scaleMin = d.ScaleMin; _scaleMax = d.ScaleMax; _sizeX = d.SizeX; _sizeY = d.SizeY;
    }

    public override void ApplyState(BaseUIElementData data) { var d = (PlotHistogramData)data; Data = d; Name = d.Name; CopyFrom(d); }

    public override void Render()
    {
        if (_values == null || _values.Length == 0) return;
        var sMin = _scaleMin == float.MaxValue ? float.MaxValue : _scaleMin;
        var sMax = _scaleMax == float.MaxValue ? float.MaxValue : _scaleMax;
        ImGui.PlotHistogram(Data.Name, ref _values[0], _values.Length, _offset, _overlayText, sMin, sMax, new Vector2(_sizeX, _sizeY));
    }

    public override BaseUIElementData? GetNewState() => null;
}
