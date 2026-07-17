namespace lstwoMODS.ImGui.Shared.UI
{
    public class ImPlotHistogramData : BaseUIElementData
    {
        public float[]  Values   { get; set; } = System.Array.Empty<float>();
        public int      Bins     { get; set; } = (int)ImPlotBin.Sturges;
        public double   RangeMin { get; set; } = 0.0;
        public double   RangeMax { get; set; } = 1.0;
        public bool     HasRange { get; set; } = false;
        public ImPlotHistogramFlags Flags { get; set; } = ImPlotHistogramFlags.None;
    }
}
