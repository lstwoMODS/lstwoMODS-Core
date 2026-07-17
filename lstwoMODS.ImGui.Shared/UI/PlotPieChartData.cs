namespace lstwoMODS.ImGui.Shared.UI
{
    public class PlotPieChartData : BaseUIElementData
    {
        public float[]   Values   { get; set; } = System.Array.Empty<float>();
        public string[]  Labels   { get; set; } = System.Array.Empty<string>();
        public double    X        { get; set; } = 0.5;
        public double    Y        { get; set; } = 0.5;
        public double    Radius   { get; set; } = 0.5;
        public string    LabelFmt { get; set; } = "%.1f";
        public double    Angle0   { get; set; } = 90.0;
        public ImPlotPieChartFlags Flags { get; set; } = ImPlotPieChartFlags.None;
    }
}
