using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;
public class PlotBarSeries : BaseUIElement<PlotBarSeries>
{
    public float[] XValues { get => ((PlotBarsSeriesData)Data).XValues; set { ((PlotBarsSeriesData)Data).XValues = value; MarkChanged(); } }
    public float[] YValues { get => ((PlotBarsSeriesData)Data).YValues; set { ((PlotBarsSeriesData)Data).YValues = value; MarkChanged(); } }

    public PlotBarSeries(string name, float[] xValues, float[] yValues, double barWidth = 0.67, ImPlotBarsFlags flags = ImPlotBarsFlags.None) : base(name)
    {
        Data = new PlotBarsSeriesData { Name = name, XValues = xValues ?? System.Array.Empty<float>(), YValues = yValues ?? System.Array.Empty<float>(), BarWidth = barWidth, Flags = flags };
    }

    public PlotBarSeries WithBarWidth(double width) { ((PlotBarsSeriesData)Data).BarWidth = width; return this; }
    public PlotBarSeries WithFlags(ImPlotBarsFlags flags) { ((PlotBarsSeriesData)Data).Flags = flags; return this; }
}
