namespace lstwoMODS.ImGui.Shared.UI
{
    public class PlotStairsSeriesData : BaseUIElementData
    {
        public float[] XValues { get; set; } = System.Array.Empty<float>();
        public float[] YValues { get; set; } = System.Array.Empty<float>();
        public ImPlotStairsFlags Flags { get; set; } = ImPlotStairsFlags.None;
    }
}
