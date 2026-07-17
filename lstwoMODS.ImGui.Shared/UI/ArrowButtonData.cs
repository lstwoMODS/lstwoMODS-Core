namespace lstwoMODS.ImGui.Shared.UI
{
    public class ArrowButtonData : BaseUIElementData
    {
        public ImGuiDir Dir     { get; set; } = ImGuiDir.Right;
        public bool     Pressed { get; set; } = false;
    }
}
