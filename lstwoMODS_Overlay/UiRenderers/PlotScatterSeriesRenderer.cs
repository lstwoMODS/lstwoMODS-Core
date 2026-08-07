using Hexa.NET.ImPlot;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class PlotScatterSeriesRenderer : UIRenderer
{
    private float[] _xs, _ys;
    private ImPlotScatterFlags _flags;

    public PlotScatterSeriesRenderer(BaseUIElementData data) : base(data) { CopyFrom((PlotScatterSeriesData)data); }

    private void CopyFrom(PlotScatterSeriesData d)
    {
        _xs    = d.XValues;
        _ys    = d.YValues;
        _flags = (ImPlotScatterFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (PlotScatterSeriesData)data;
        Data = d;
        Name = d.Name;
        CopyFrom(d);
    }

    public override void Render()
    {
        if (_xs == null || _ys == null || _xs.Length == 0) return;
        ImPlot.PlotScatter(Data.Name, ref _xs[0], ref _ys[0], _xs.Length, _flags);
    }

    public override BaseUIElementData? GetNewState() => null;
}
