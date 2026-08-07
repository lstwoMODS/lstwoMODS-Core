using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;
public class Plot3DScatterSeries : BaseUIElement<Plot3DScatterSeries>
{
    public Plot3DScatterSeries(string name, float[] xs, float[] ys, float[] zs, ImPlot3DScatterFlags flags = ImPlot3DScatterFlags.None) : base(name)
    {
        Data = new Plot3DScatterSeriesData { Name = name, XValues = xs ?? System.Array.Empty<float>(), YValues = ys ?? System.Array.Empty<float>(), ZValues = zs ?? System.Array.Empty<float>(), Flags = flags };
    }

    public void Update(float[] xs, float[] ys, float[] zs) { var d = (Plot3DScatterSeriesData)Data; d.XValues = xs; d.YValues = ys; d.ZValues = zs; MarkChanged(); }
    public Plot3DScatterSeries WithFlags(ImPlot3DScatterFlags flags) { ((Plot3DScatterSeriesData)Data).Flags = flags; return this; }
}
