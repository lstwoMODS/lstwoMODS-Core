using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;
public class PlotLineSeries : BaseUIElement<PlotLineSeries>
{
    public float[] XValues { get => ((PlotLineSeriesData)Data).XValues; set { ((PlotLineSeriesData)Data).XValues = value; MarkChanged(); } }
    public float[] YValues { get => ((PlotLineSeriesData)Data).YValues; set { ((PlotLineSeriesData)Data).YValues = value; MarkChanged(); } }

    public PlotLineSeries(string name, float[] xValues, float[] yValues, ImPlotLineFlags flags = ImPlotLineFlags.None) : base(name)
    {
        Data = new PlotLineSeriesData { Name = name, XValues = xValues ?? System.Array.Empty<float>(), YValues = yValues ?? System.Array.Empty<float>(), Flags = flags };
    }

    public PlotLineSeries WithFlags(ImPlotLineFlags flags) { ((PlotLineSeriesData)Data).Flags = flags; return this; }
    public void Update(float[] xs, float[] ys) { XValues = xs; YValues = ys; }
}
