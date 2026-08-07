using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;
public class PlotPieChart : BaseUIElement<PlotPieChart>
{
    public float[]  Values { get => ((PlotPieChartData)Data).Values; set { ((PlotPieChartData)Data).Values = value; MarkChanged(); } }
    public string[] Labels { get => ((PlotPieChartData)Data).Labels; set { ((PlotPieChartData)Data).Labels = value; MarkChanged(); } }

    public PlotPieChart(string name, float[] values, string[] labels, double x = 0.5, double y = 0.5, double radius = 0.5, string labelFmt = "%.1f", ImPlotPieChartFlags flags = ImPlotPieChartFlags.None) : base(name)
    {
        Data = new PlotPieChartData { Name = name, Values = values ?? System.Array.Empty<float>(), Labels = labels ?? System.Array.Empty<string>(), X = x, Y = y, Radius = radius, LabelFmt = labelFmt, Flags = flags };
    }

    public PlotPieChart WithFlags(ImPlotPieChartFlags flags) { ((PlotPieChartData)Data).Flags = flags; return this; }
}
