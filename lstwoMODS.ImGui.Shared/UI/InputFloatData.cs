namespace lstwoMODS.ImGui.Shared.UI
{
    public class InputFloatData : BaseUIElementData
    {
        public float Value { get; set; } = 0f;
        public float Step { get; set; } = 0f;
        public float StepFast { get; set; } = 0f;
        public string Format { get; set; } = "%.3f";
        public ImGuiInputTextFlags Flags { get; set; } = ImGuiInputTextFlags.None;
    }
}
