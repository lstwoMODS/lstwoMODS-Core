namespace lstwoMODS.ImGui.Shared.UI
{
    public class PlotHeatmapData : BaseUIElementData
    {
        public float[]  Values   { get; set; } = System.Array.Empty<float>(); // flattened row-major
        public int      Rows     { get; set; } = 1;
        public int      Cols     { get; set; } = 1;
        public double   ScaleMin { get; set; } = 0.0;
        public double   ScaleMax { get; set; } = 1.0;
        public string   LabelFmt { get; set; } = "%.1f";
        public ImPlotHeatmapFlags Flags { get; set; } = ImPlotHeatmapFlags.None;
    }
}
