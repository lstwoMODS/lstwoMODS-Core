namespace lstwoMODS.ImGui.Shared.UI
{
    public class ColorEdit4Data : BaseUIElementData
    {
        public float R { get; set; } = 1f;
        public float G { get; set; } = 1f;
        public float B { get; set; } = 1f;
        public float A { get; set; } = 1f;
        public ImGuiColorEditFlags Flags { get; set; } = ImGuiColorEditFlags.None;
    }
}
