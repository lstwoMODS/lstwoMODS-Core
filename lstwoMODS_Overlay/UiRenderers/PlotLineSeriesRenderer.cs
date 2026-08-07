using Hexa.NET.ImPlot;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class PlotLineSeriesRenderer : UIRenderer
{
    private float[] _xs, _ys;
    private ImPlotLineFlags _flags;

    public PlotLineSeriesRenderer(BaseUIElementData data) : base(data) { CopyFrom((PlotLineSeriesData)data); }

    private void CopyFrom(PlotLineSeriesData d)
    {
        _xs    = d.XValues;
        _ys    = d.YValues;
        _flags = (ImPlotLineFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (PlotLineSeriesData)data;
        Data = d;
        Name = d.Name;
        CopyFrom(d);
    }

    public override void Render()
    {
        if (_xs == null || _ys == null || _xs.Length == 0) return;
        ImPlot.PlotLine(Data.Name, ref _xs[0], ref _ys[0], _xs.Length, _flags);
    }

    public override BaseUIElementData? GetNewState() => null;
}
