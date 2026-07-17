namespace lstwoMODS.ImGui.Shared.UI
{
    public class PlotAnnotationData : BaseUIElementData
    {
        public double X       { get; set; } = 0.0;
        public double Y       { get; set; } = 0.0;
        public float  PixOffX { get; set; } = 0f;
        public float  PixOffY { get; set; } = 0f;
        public string Text    { get; set; } = "";
        public bool   Clamp   { get; set; } = false;
        public float  ColorR  { get; set; } = 0f;
        public float  ColorG  { get; set; } = 0f;
        public float  ColorB  { get; set; } = 0f;
        public float  ColorA  { get; set; } = 0f; // 0 = auto color
    }
}
