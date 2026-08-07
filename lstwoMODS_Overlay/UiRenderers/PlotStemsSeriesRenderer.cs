using Hexa.NET.ImPlot;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class PlotStemsSeriesRenderer : UIRenderer
{
    private float[] _xs, _ys;
    private double _ref;
    private ImPlotStemsFlags _flags;

    public PlotStemsSeriesRenderer(BaseUIElementData data) : base(data) { CopyFrom((PlotStemsSeriesData)data); }

    private void CopyFrom(PlotStemsSeriesData d)
    {
        _xs    = d.XValues;
        _ys    = d.YValues;
        _ref   = d.Ref;
        _flags = (ImPlotStemsFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (PlotStemsSeriesData)data;
        Data = d;
        Name = d.Name;
        CopyFrom(d);
    }

    public override void Render()
    {
        if (_xs == null || _ys == null || _xs.Length == 0) return;
        ImPlot.PlotStems(Data.Name, ref _xs[0], ref _ys[0], _xs.Length, _ref, _flags);
    }

    public override BaseUIElementData? GetNewState() => null;
}
