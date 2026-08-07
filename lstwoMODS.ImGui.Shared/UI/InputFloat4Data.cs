namespace lstwoMODS.ImGui.Shared.UI
{
    public class InputFloat4Data : BaseUIElementData
    {
        public float X { get; set; } = 0f;
        public float Y { get; set; } = 0f;
        public float Z { get; set; } = 0f;
        public float W { get; set; } = 0f;
        public string Format { get; set; } = "%.3f";
        public ImGuiInputTextFlags Flags { get; set; } = ImGuiInputTextFlags.None;
    }
}
