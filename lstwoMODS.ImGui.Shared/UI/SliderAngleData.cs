namespace lstwoMODS.ImGui.Shared.UI
{
    public class SliderAngleData : BaseUIElementData
    {
        public float AngleRad { get; set; } = 0f;
        public float MinDegrees { get; set; } = -360f;
        public float MaxDegrees { get; set; } = 360f;
        public string Format { get; set; } = "%.0f deg";
        public ImGuiSliderFlags Flags { get; set; } = ImGuiSliderFlags.None;
    }
}
