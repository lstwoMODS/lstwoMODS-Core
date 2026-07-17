using System.Collections.Generic;
using System.Numerics;
using Hexa.NET.ImPlot;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class PlotPanelRenderer : UIRenderer
{
    private string _title;
    private float _sizeX, _sizeY;
    private ImPlotFlags _flags;
    private string _xLabel, _yLabel;
    private ImPlotAxisFlags _xFlags, _yFlags;
    private bool _hasLimits;
    private double _xMin, _xMax, _yMin, _yMax;
    private ImPlotCond _limitsCond;
    private bool _hasLegend;
    private ImPlotLocation _legendLoc;
    private ImPlotLegendFlags _legendFlags;
    private List<BaseUIElementData> _children;

    public PlotPanelRenderer(BaseUIElementData data) : base(data) { CopyFrom((PlotPanelData)data); }

    private void CopyFrom(PlotPanelData d)
    {
        _title       = d.Title;
        _sizeX       = d.SizeX;
        _sizeY       = d.SizeY;
        _flags       = (ImPlotFlags)(int)d.Flags;
        _xLabel      = d.XLabel;
        _yLabel      = d.YLabel;
        _xFlags      = (ImPlotAxisFlags)(int)d.XFlags;
        _yFlags      = (ImPlotAxisFlags)(int)d.YFlags;
        _hasLimits   = d.HasLimits;
        _xMin        = d.XMin;
        _xMax        = d.XMax;
        _yMin        = d.YMin;
        _yMax        = d.YMax;
        _limitsCond  = (ImPlotCond)(int)d.LimitsCond;
        _hasLegend   = d.HasLegend;
        _legendLoc   = (ImPlotLocation)(int)d.LegendLocation;
        _legendFlags = (ImPlotLegendFlags)(int)d.LegendFlags;
        _children    = d.Children;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (PlotPanelData)data;
        var prev = _children;
        Data = d;
        Name = d.Name;
        CopyFrom(d);
        if (!(d.Children?.Count > 0)) _children = prev;
    }

    public override void Render()
    {
        if (!ImPlot.BeginPlot(_title, new Vector2(_sizeX, _sizeY), _flags)) return;

        if (_xLabel != null || _xFlags != 0)
            ImPlot.SetupAxis(ImAxis.X1, _xLabel, _xFlags);
        if (_yLabel != null || _yFlags != 0)
            ImPlot.SetupAxis(ImAxis.Y1, _yLabel, _yFlags);
        if (_hasLimits)
            ImPlot.SetupAxesLimits(_xMin, _xMax, _yMin, _yMax, _limitsCond);
        if (_hasLegend)
            ImPlot.SetupLegend(_legendLoc, _legendFlags);

        foreach (var child in _children)
            Window.RenderSingleElement(child);

        ImPlot.EndPlot();
    }

    public override BaseUIElementData? GetNewState() => null;
}
