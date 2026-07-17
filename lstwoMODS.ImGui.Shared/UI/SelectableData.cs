namespace lstwoMODS.ImGui.Shared.UI
{
    public class SelectableData : BaseUIElementData
    {
        public bool                 Selected { get; set; } = false;
        public float                SizeX    { get; set; } = 0f;
        public float                SizeY    { get; set; } = 0f;
        public ImGuiSelectableFlags Flags    { get; set; } = ImGuiSelectableFlags.None;
    }
}
