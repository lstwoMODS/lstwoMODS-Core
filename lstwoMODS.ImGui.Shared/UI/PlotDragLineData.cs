namespace lstwoMODS.ImGui.Shared.UI
{
    public class PlotDragLineData : BaseUIElementData
    {
        public int    DragId    { get; set; } = 0;
        public bool   Vertical  { get; set; } = true;  // true=DragLineX, false=DragLineY
        public double Value     { get; set; } = 0.0;   // x or y position; updated on drag
        public float  ColorR    { get; set; } = 1f;
        public float  ColorG    { get; set; } = 0f;
        public float  ColorB    { get; set; } = 0f;
        public float  ColorA    { get; set; } = 1f;
        public float  Thickness { get; set; } = 1f;
        public ImPlotDragToolFlags Flags { get; set; } = ImPlotDragToolFlags.None;
        public bool Clicked { get; set; } = false;  // set by overlay on click
    }
}
