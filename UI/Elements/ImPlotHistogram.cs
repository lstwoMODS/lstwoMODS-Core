using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;
public class ImPlotHistogram : BaseUIElement<ImPlotHistogram>
{
    public float[] Values { get => ((ImPlotHistogramData)Data).Values; set { ((ImPlotHistogramData)Data).Values = value; MarkChanged(); } }

    public ImPlotHistogram(string name, float[] values, int bins = (int)ImPlotBin.Sturges, ImPlotHistogramFlags flags = ImPlotHistogramFlags.None) : base(name)
    {
        Data = new ImPlotHistogramData { Name = name, Values = values ?? System.Array.Empty<float>(), Bins = bins, Flags = flags };
    }

    public ImPlotHistogram WithRange(double min, double max) { var d = (ImPlotHistogramData)Data; d.HasRange = true; d.RangeMin = min; d.RangeMax = max; return this; }
    public ImPlotHistogram WithFlags(ImPlotHistogramFlags flags) { ((ImPlotHistogramData)Data).Flags = flags; return this; }
}
