namespace lstwoMODS.ImGui.Shared.UI
{
    public class InputIntData : BaseUIElementData
    {
        public int Value { get; set; } = 0;
        public int Step { get; set; } = 1;
        public int StepFast { get; set; } = 100;
        public ImGuiInputTextFlags Flags { get; set; } = ImGuiInputTextFlags.None;
    }
}
