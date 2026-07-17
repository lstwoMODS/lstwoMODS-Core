using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;
public class PlotStemsSeries : BaseUIElement<PlotStemsSeries>
{
    public float[] XValues { get => ((PlotStemsSeriesData)Data).XValues; set { ((PlotStemsSeriesData)Data).XValues = value; MarkChanged(); } }
    public float[] YValues { get => ((PlotStemsSeriesData)Data).YValues; set { ((PlotStemsSeriesData)Data).YValues = value; MarkChanged(); } }
    public double  Ref     { get => ((PlotStemsSeriesData)Data).Ref;     set { ((PlotStemsSeriesData)Data).Ref = value; MarkChanged(); } }

    public PlotStemsSeries(string name, float[] xValues, float[] yValues, double baseline = 0.0, ImPlotStemsFlags flags = ImPlotStemsFlags.None) : base(name)
    {
        Data = new PlotStemsSeriesData { Name = name, XValues = xValues ?? System.Array.Empty<float>(), YValues = yValues ?? System.Array.Empty<float>(), Ref = baseline, Flags = flags };
    }

    public PlotStemsSeries WithBaseline(double baseline) { ((PlotStemsSeriesData)Data).Ref = baseline; return this; }
    public PlotStemsSeries WithFlags(ImPlotStemsFlags flags) { ((PlotStemsSeriesData)Data).Flags = flags; return this; }
    public void Update(float[] xs, float[] ys) { XValues = xs; YValues = ys; }
}
