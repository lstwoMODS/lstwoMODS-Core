using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;
public class PlotScatterSeries : BaseUIElement<PlotScatterSeries>
{
    public float[] XValues { get => ((PlotScatterSeriesData)Data).XValues; set { ((PlotScatterSeriesData)Data).XValues = value; MarkChanged(); } }
    public float[] YValues { get => ((PlotScatterSeriesData)Data).YValues; set { ((PlotScatterSeriesData)Data).YValues = value; MarkChanged(); } }

    public PlotScatterSeries(string name, float[] xValues, float[] yValues, ImPlotScatterFlags flags = ImPlotScatterFlags.None) : base(name)
    {
        Data = new PlotScatterSeriesData { Name = name, XValues = xValues ?? System.Array.Empty<float>(), YValues = yValues ?? System.Array.Empty<float>(), Flags = flags };
    }

    public PlotScatterSeries WithFlags(ImPlotScatterFlags flags) { ((PlotScatterSeriesData)Data).Flags = flags; return this; }
    public void Update(float[] xs, float[] ys) { XValues = xs; YValues = ys; }
}
