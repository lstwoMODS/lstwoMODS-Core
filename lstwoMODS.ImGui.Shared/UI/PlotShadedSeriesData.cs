namespace lstwoMODS.ImGui.Shared.UI
{
    public class PlotShadedSeriesData : BaseUIElementData
    {
        public float[] XValues  { get; set; } = System.Array.Empty<float>();
        public float[] YValues1 { get; set; } = System.Array.Empty<float>();
        public float[] YValues2 { get; set; } = null; // null = shade to y=0
        public ImPlotShadedFlags Flags { get; set; } = ImPlotShadedFlags.None;
    }
}
