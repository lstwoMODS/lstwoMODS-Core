using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;
public class PlotPanel : BaseUIElement<PlotPanel>
{
    public List<BaseUIElement> Children;

    public PlotPanel(string name, string title, float sizeX = -1f, float sizeY = 300f, params BaseUIElement[] children) : base(name)
    {
        Children = new List<BaseUIElement>(children);
        Data = new PlotPanelData
        {
            Name = name, Title = title, SizeX = sizeX, SizeY = sizeY,
            Children = Children.Select(c => c.Data).ToList()
        };
    }

    public PlotPanel WithFlags(ImPlotFlags flags)          { ((PlotPanelData)Data).Flags = flags; return this; }
    public PlotPanel WithXAxis(string label = null, ImPlotAxisFlags flags = ImPlotAxisFlags.None)
    { var d = (PlotPanelData)Data; d.XLabel = label; d.XFlags = flags; return this; }
    public PlotPanel WithYAxis(string label = null, ImPlotAxisFlags flags = ImPlotAxisFlags.None)
    { var d = (PlotPanelData)Data; d.YLabel = label; d.YFlags = flags; return this; }
    public PlotPanel WithLimits(double xMin, double xMax, double yMin, double yMax, ImPlotCond cond = ImPlotCond.Once)
    { var d = (PlotPanelData)Data; d.HasLimits = true; d.XMin = xMin; d.XMax = xMax; d.YMin = yMin; d.YMax = yMax; d.LimitsCond = cond; return this; }
    public PlotPanel WithLegend(ImPlotLocation location = ImPlotLocation.NorthWest, ImPlotLegendFlags flags = ImPlotLegendFlags.None)
    { var d = (PlotPanelData)Data; d.HasLegend = true; d.LegendLocation = location; d.LegendFlags = flags; return this; }

    public override IEnumerable<BaseUIElement> GetChildren() => Children;
}
