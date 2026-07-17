using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;
public class PlotShadedSeries : BaseUIElement<PlotShadedSeries>
{
    public PlotShadedSeries(string name, float[] xs, float[] ys1, float[] ys2 = null, ImPlotShadedFlags flags = ImPlotShadedFlags.None) : base(name)
    {
        Data = new PlotShadedSeriesData { Name = name, XValues = xs ?? System.Array.Empty<float>(), YValues1 = ys1 ?? System.Array.Empty<float>(), YValues2 = ys2, Flags = flags };
    }

    public PlotShadedSeries WithFlags(ImPlotShadedFlags flags) { ((PlotShadedSeriesData)Data).Flags = flags; return this; }
    public void Update(float[] xs, float[] ys1, float[] ys2 = null) { var d = (PlotShadedSeriesData)Data; d.XValues = xs; d.YValues1 = ys1; d.YValues2 = ys2; MarkChanged(); }
}
