namespace lstwoMODS.ImGui.Shared.UI
{
    public class SliderIntData : BaseUIElementData
    {
        public int Value { get; set; } = 0;
        public int Min { get; set; } = 0;
        public int Max { get; set; } = 100;
        public string Format { get; set; } = "%d";
        public ImGuiSliderFlags Flags { get; set; } = ImGuiSliderFlags.None;
    }
}
