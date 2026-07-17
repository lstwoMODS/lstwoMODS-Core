namespace lstwoMODS.ImGui.Shared.UI
{
    public class SliderFloatData : BaseUIElementData
    {
        public float Value { get; set; } = 0f;
        public float Min { get; set; } = 0f;
        public float Max { get; set; } = 1f;
        public string Format { get; set; } = "%.3f";
        public ImGuiSliderFlags Flags { get; set; } = ImGuiSliderFlags.None;
    }
}
