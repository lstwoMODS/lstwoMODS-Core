using Hexa.NET.ImPlot;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class PlotHeatmapRenderer : UIRenderer
{
    private float[] _values;
    private int _rows, _cols;
    private double _scaleMin, _scaleMax;
    private string _labelFmt;
    private ImPlotHeatmapFlags _flags;

    public PlotHeatmapRenderer(BaseUIElementData data) : base(data) { CopyFrom((PlotHeatmapData)data); }

    private void CopyFrom(PlotHeatmapData d)
    {
        _values   = d.Values;
        _rows     = d.Rows;
        _cols     = d.Cols;
        _scaleMin = d.ScaleMin;
        _scaleMax = d.ScaleMax;
        _labelFmt = d.LabelFmt;
        _flags    = (ImPlotHeatmapFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (PlotHeatmapData)data;
        Data = d;
        Name = d.Name;
        CopyFrom(d);
    }

    public override void Render()
    {
        if (_values == null || _values.Length == 0) return;
        ImPlot.PlotHeatmap(Data.Name, ref _values[0], _rows, _cols, _scaleMin, _scaleMax, _labelFmt,
            new ImPlotPoint(0, 0), new ImPlotPoint(1, 1), _flags);
    }

    public override BaseUIElementData? GetNewState() => null;
}
