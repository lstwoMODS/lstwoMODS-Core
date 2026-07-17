namespace lstwoMODS.ImGui.Shared.UI
{
    public class Plot3DLineSeriesData : BaseUIElementData
    {
        public float[] XValues { get; set; } = System.Array.Empty<float>();
        public float[] YValues { get; set; } = System.Array.Empty<float>();
        public float[] ZValues { get; set; } = System.Array.Empty<float>();
        public ImPlot3DLineFlags Flags { get; set; } = ImPlot3DLineFlags.None;
    }
}
