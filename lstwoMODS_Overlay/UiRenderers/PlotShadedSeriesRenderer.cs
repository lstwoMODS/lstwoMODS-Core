using Hexa.NET.ImPlot;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class PlotShadedSeriesRenderer : UIRenderer
{
    private float[] _xs, _ys1, _ys2;
    private ImPlotShadedFlags _flags;

    public PlotShadedSeriesRenderer(BaseUIElementData data) : base(data) { CopyFrom((PlotShadedSeriesData)data); }

    private void CopyFrom(PlotShadedSeriesData d)
    {
        _xs    = d.XValues;
        _ys1   = d.YValues1;
        _ys2   = d.YValues2;
        _flags = (ImPlotShadedFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (PlotShadedSeriesData)data;
        Data = d;
        Name = d.Name;
        CopyFrom(d);
    }

    public override void Render()
    {
        if (_xs == null || _ys1 == null || _xs.Length == 0) return;
        if (_ys2 != null && _ys2.Length == _xs.Length)
            ImPlot.PlotShaded(Data.Name, ref _xs[0], ref _ys1[0], ref _ys2[0], _xs.Length, _flags);
        else
            ImPlot.PlotShaded(Data.Name, ref _xs[0], ref _ys1[0], _xs.Length, 0.0, _flags);
    }

    public override BaseUIElementData? GetNewState() => null;
}
