namespace lstwoMODS.ImGui.Shared.UI
{
    public class DragFloat3Data : BaseUIElementData
    {
        public float X { get; set; } = 0f;
        public float Y { get; set; } = 0f;
        public float Z { get; set; } = 0f;
        public float Speed { get; set; } = 1f;
        public float Min { get; set; } = 0f;
        public float Max { get; set; } = 0f;
        public string Format { get; set; } = "%.3f";
        public ImGuiSliderFlags Flags { get; set; } = ImGuiSliderFlags.None;
    }
}
