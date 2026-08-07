namespace lstwoMODS.ImGui.Shared.UI
{
    public class DragIntData : BaseUIElementData
    {
        public int Value { get; set; } = 0;
        public float Speed { get; set; } = 1f;
        public int Min { get; set; } = 0;
        public int Max { get; set; } = 0;
        public string Format { get; set; } = "%d";
        public ImGuiSliderFlags Flags { get; set; } = ImGuiSliderFlags.None;
    }
}
