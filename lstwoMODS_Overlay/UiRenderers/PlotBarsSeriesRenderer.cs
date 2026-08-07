using Hexa.NET.ImPlot;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class PlotBarsSeriesRenderer : UIRenderer
{
    private float[] _xs, _ys;
    private double _barWidth;
    private ImPlotBarsFlags _flags;

    public PlotBarsSeriesRenderer(BaseUIElementData data) : base(data) { CopyFrom((PlotBarsSeriesData)data); }

    private void CopyFrom(PlotBarsSeriesData d)
    {
        _xs       = d.XValues;
        _ys       = d.YValues;
        _barWidth = d.BarWidth;
        _flags    = (ImPlotBarsFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (PlotBarsSeriesData)data;
        Data = d;
        Name = d.Name;
        CopyFrom(d);
    }

    public override void Render()
    {
        if (_xs == null || _ys == null || _xs.Length == 0) return;
        ImPlot.PlotBars(Data.Name, ref _xs[0], ref _ys[0], _xs.Length, _barWidth, _flags);
    }

    public override BaseUIElementData? GetNewState() => null;
}
