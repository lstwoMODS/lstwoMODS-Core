namespace lstwoMODS.ImGui.Shared.UI
{
    public class PlotScatterSeriesData : BaseUIElementData
    {
        public float[] XValues { get; set; } = System.Array.Empty<float>();
        public float[] YValues { get; set; } = System.Array.Empty<float>();
        public ImPlotScatterFlags Flags { get; set; } = ImPlotScatterFlags.None;
    }
}
