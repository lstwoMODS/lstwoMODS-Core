namespace lstwoMODS.ImGui.Shared.UI
{
    public class ProgressBarData : BaseUIElementData
    {
        public float  Value   { get; set; } = 0f;
        public float  SizeX   { get; set; } = -1f; // -1 = fill available width
        public float  SizeY   { get; set; } = 0f;
        public string Overlay { get; set; } = null; // null = default percentage text
    }
}
