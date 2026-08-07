namespace lstwoMODS.ImGui.Shared.UI
{
    public class PlotLineSeriesData : BaseUIElementData
    {
        public float[] XValues { get; set; } = System.Array.Empty<float>();
        public float[] YValues { get; set; } = System.Array.Empty<float>();
        public ImPlotLineFlags Flags { get; set; } = ImPlotLineFlags.None;
    }
}
