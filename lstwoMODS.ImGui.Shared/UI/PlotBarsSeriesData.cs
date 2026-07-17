namespace lstwoMODS.ImGui.Shared.UI
{
    public class PlotBarsSeriesData : BaseUIElementData
    {
        public float[] XValues  { get; set; } = System.Array.Empty<float>();
        public float[] YValues  { get; set; } = System.Array.Empty<float>();
        public double  BarWidth { get; set; } = 0.67;
        public ImPlotBarsFlags Flags { get; set; } = ImPlotBarsFlags.None;
    }
}
