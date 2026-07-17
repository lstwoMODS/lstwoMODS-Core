namespace lstwoMODS.ImGui.Shared.UI
{
    public class InputInt3Data : BaseUIElementData
    {
        public int X { get; set; } = 0;
        public int Y { get; set; } = 0;
        public int Z { get; set; } = 0;
        public ImGuiInputTextFlags Flags { get; set; } = ImGuiInputTextFlags.None;
    }
}
