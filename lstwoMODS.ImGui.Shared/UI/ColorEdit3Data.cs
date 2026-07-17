namespace lstwoMODS.ImGui.Shared.UI
{
    public class ColorEdit3Data : BaseUIElementData
    {
        public float R { get; set; } = 1f;
        public float G { get; set; } = 1f;
        public float B { get; set; } = 1f;
        public ImGuiColorEditFlags Flags { get; set; } = ImGuiColorEditFlags.None;
    }
}
