namespace lstwoMODS.ImGui.Shared.UI
{
    public class DockSpaceData : BaseUIElementData
    {
        public uint              DockSpaceId { get; set; }
        public ImGuiDockNodeFlags Flags       { get; set; } = ImGuiDockNodeFlags.None;
    }
}
