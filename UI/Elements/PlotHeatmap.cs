using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;
public class PlotHeatmap : BaseUIElement<PlotHeatmap>
{
    public float[] Values { get => ((PlotHeatmapData)Data).Values; set { ((PlotHeatmapData)Data).Values = value; MarkChanged(); } }

    public PlotHeatmap(string name, float[] values, int rows, int cols, double scaleMin = 0, double scaleMax = 1, string labelFmt = "%.1f", ImPlotHeatmapFlags flags = ImPlotHeatmapFlags.None) : base(name)
    {
        Data = new PlotHeatmapData { Name = name, Values = values ?? System.Array.Empty<float>(), Rows = rows, Cols = cols, ScaleMin = scaleMin, ScaleMax = scaleMax, LabelFmt = labelFmt, Flags = flags };
    }

    public PlotHeatmap WithScale(double min, double max) { var d = (PlotHeatmapData)Data; d.ScaleMin = min; d.ScaleMax = max; return this; }
    public PlotHeatmap WithFlags(ImPlotHeatmapFlags flags) { ((PlotHeatmapData)Data).Flags = flags; return this; }
}
