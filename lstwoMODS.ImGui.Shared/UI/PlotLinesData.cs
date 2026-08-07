namespace lstwoMODS.ImGui.Shared.UI
{
    public class PlotLinesData : BaseUIElementData
    {
        public float[] Values      { get; set; } = System.Array.Empty<float>();
        public int     Offset      { get; set; } = 0;
        public string  OverlayText { get; set; } = null;
        public float   ScaleMin    { get; set; } = float.MaxValue;  // float.MaxValue = auto
        public float   ScaleMax    { get; set; } = float.MaxValue;
        public float   SizeX       { get; set; } = 0f;
        public float   SizeY       { get; set; } = 80f;
    }

    public class PlotHistogramData : BaseUIElementData
    {
        public float[] Values      { get; set; } = System.Array.Empty<float>();
        public int     Offset      { get; set; } = 0;
        public string  OverlayText { get; set; } = null;
        public float   ScaleMin    { get; set; } = float.MaxValue;
        public float   ScaleMax    { get; set; } = float.MaxValue;
        public float   SizeX       { get; set; } = 0f;
        public float   SizeY       { get; set; } = 80f;
    }
}
