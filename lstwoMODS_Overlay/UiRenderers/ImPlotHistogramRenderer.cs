using Hexa.NET.ImPlot;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class ImPlotHistogramRenderer : UIRenderer
{
    private float[] _values;
    private int _bins;
    private double _rangeMin, _rangeMax;
    private bool _hasRange;
    private ImPlotHistogramFlags _flags;

    public ImPlotHistogramRenderer(BaseUIElementData data) : base(data) { CopyFrom((ImPlotHistogramData)data); }

    private void CopyFrom(ImPlotHistogramData d)
    {
        _values   = d.Values;
        _bins     = d.Bins;
        _rangeMin = d.RangeMin;
        _rangeMax = d.RangeMax;
        _hasRange = d.HasRange;
        _flags    = (ImPlotHistogramFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (ImPlotHistogramData)data;
        Data = d;
        Name = d.Name;
        CopyFrom(d);
    }

    public override void Render()
    {
        if (_values == null || _values.Length == 0) return;
        var range = _hasRange
            ? new ImPlotRange(_rangeMin, _rangeMax)
            : new ImPlotRange(double.NegativeInfinity, double.PositiveInfinity);
        // Signature: PlotHistogram(label, float* values, count, bins, barScale:double, range, flags)
        // Cumulative/density are controlled via ImPlotHistogramFlags, not a bool parameter
        ImPlot.PlotHistogram(Data.Name, ref _values[0], _values.Length, _bins, 1.0, range, _flags);
    }

    public override BaseUIElementData? GetNewState() => null;
}
