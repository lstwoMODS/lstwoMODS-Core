using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;
public class Plot3DLineSeries : BaseUIElement<Plot3DLineSeries>
{
    public Plot3DLineSeries(string name, float[] xs, float[] ys, float[] zs, ImPlot3DLineFlags flags = ImPlot3DLineFlags.None) : base(name)
    {
        Data = new Plot3DLineSeriesData { Name = name, XValues = xs ?? System.Array.Empty<float>(), YValues = ys ?? System.Array.Empty<float>(), ZValues = zs ?? System.Array.Empty<float>(), Flags = flags };
    }

    public void Update(float[] xs, float[] ys, float[] zs) { var d = (Plot3DLineSeriesData)Data; d.XValues = xs; d.YValues = ys; d.ZValues = zs; MarkChanged(); }
    public Plot3DLineSeries WithFlags(ImPlot3DLineFlags flags) { ((Plot3DLineSeriesData)Data).Flags = flags; return this; }
}
