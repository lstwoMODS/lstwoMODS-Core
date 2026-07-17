using Hexa.NET.ImPlot;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class PlotStairsSeriesRenderer : UIRenderer
{
    private float[] _xs, _ys;
    private ImPlotStairsFlags _flags;

    public PlotStairsSeriesRenderer(BaseUIElementData data) : base(data) { CopyFrom((PlotStairsSeriesData)data); }

    private void CopyFrom(PlotStairsSeriesData d)
    {
        _xs    = d.XValues;
        _ys    = d.YValues;
        _flags = (ImPlotStairsFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (PlotStairsSeriesData)data;
        Data = d;
        Name = d.Name;
        CopyFrom(d);
    }

    public override void Render()
    {
        if (_xs == null || _ys == null || _xs.Length == 0) return;
        ImPlot.PlotStairs(Data.Name, ref _xs[0], ref _ys[0], _xs.Length, _flags);
    }

    public override BaseUIElementData? GetNewState() => null;
}
