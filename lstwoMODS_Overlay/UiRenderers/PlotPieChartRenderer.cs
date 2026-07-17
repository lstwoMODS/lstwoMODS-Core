using Hexa.NET.ImPlot;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class PlotPieChartRenderer : UIRenderer
{
    private float[] _values;
    private string[] _labels;
    private double _x, _y, _radius;
    private string _labelFmt;
    private double _angle0;
    private ImPlotPieChartFlags _flags;

    public PlotPieChartRenderer(BaseUIElementData data) : base(data) { CopyFrom((PlotPieChartData)data); }

    private void CopyFrom(PlotPieChartData d)
    {
        _values   = d.Values;
        _labels   = d.Labels;
        _x        = d.X;
        _y        = d.Y;
        _radius   = d.Radius;
        _labelFmt = d.LabelFmt;
        _angle0   = d.Angle0;
        _flags    = (ImPlotPieChartFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (PlotPieChartData)data;
        Data = d;
        Name = d.Name;
        CopyFrom(d);
    }

    public override void Render()
    {
        if (_values == null || _values.Length == 0) return;
        ImPlot.PlotPieChart(_labels, ref _values[0], _values.Length, _x, _y, _radius, _labelFmt, _angle0, _flags);
    }

    public override BaseUIElementData? GetNewState() => null;
}
