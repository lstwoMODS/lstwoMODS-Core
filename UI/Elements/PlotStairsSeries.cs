using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;
public class PlotStairsSeries : BaseUIElement<PlotStairsSeries>
{
    public float[] XValues { get => ((PlotStairsSeriesData)Data).XValues; set { ((PlotStairsSeriesData)Data).XValues = value; MarkChanged(); } }
    public float[] YValues { get => ((PlotStairsSeriesData)Data).YValues; set { ((PlotStairsSeriesData)Data).YValues = value; MarkChanged(); } }

    public PlotStairsSeries(string name, float[] xValues, float[] yValues, ImPlotStairsFlags flags = ImPlotStairsFlags.None) : base(name)
    {
        Data = new PlotStairsSeriesData { Name = name, XValues = xValues ?? System.Array.Empty<float>(), YValues = yValues ?? System.Array.Empty<float>(), Flags = flags };
    }

    public PlotStairsSeries WithFlags(ImPlotStairsFlags flags) { ((PlotStairsSeriesData)Data).Flags = flags; return this; }
    public void Update(float[] xs, float[] ys) { XValues = xs; YValues = ys; }
}
