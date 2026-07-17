namespace lstwoMODS.ImGui.Shared.UI
{
    public class PlotStemsSeriesData : BaseUIElementData
    {
        public float[] XValues { get; set; } = System.Array.Empty<float>();
        public float[] YValues { get; set; } = System.Array.Empty<float>();
        public double  Ref     { get; set; } = 0.0; // baseline
        public ImPlotStemsFlags Flags { get; set; } = ImPlotStemsFlags.None;
    }
}
