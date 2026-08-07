using System.Collections.Generic;
namespace lstwoMODS.ImGui.Shared.UI
{
    public class PlotPanelData : BaseUIElementData
    {
        public string Title    { get; set; } = "Plot";
        public float  SizeX    { get; set; } = -1f;
        public float  SizeY    { get; set; } = 300f;
        public ImPlotFlags Flags { get; set; } = ImPlotFlags.None;
        // Axis labels/flags (X1, Y1)
        public string XLabel { get; set; } = null;
        public string YLabel { get; set; } = null;
        public ImPlotAxisFlags XFlags { get; set; } = ImPlotAxisFlags.None;
        public ImPlotAxisFlags YFlags { get; set; } = ImPlotAxisFlags.None;
        // Axis limits
        public bool   HasLimits { get; set; } = false;
        public double XMin { get; set; } = 0;
        public double XMax { get; set; } = 1;
        public double YMin { get; set; } = 0;
        public double YMax { get; set; } = 1;
        public ImPlotCond LimitsCond { get; set; } = ImPlotCond.Once;
        // Legend
        public bool HasLegend { get; set; } = false;
        public ImPlotLocation LegendLocation { get; set; } = ImPlotLocation.NorthWest;
        public ImPlotLegendFlags LegendFlags { get; set; } = ImPlotLegendFlags.None;
        public List<BaseUIElementData> Children { get; set; } = new List<BaseUIElementData>();
    }
}
