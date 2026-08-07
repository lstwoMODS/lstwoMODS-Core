namespace lstwoMODS.ImGui.Shared.UI
{
    public class ComboData : BaseUIElementData
    {
        public string[]        Items         { get; set; } = System.Array.Empty<string>();
        public int             SelectedIndex { get; set; } = 0;
        public ImGuiComboFlags Flags         { get; set; } = ImGuiComboFlags.None;
    }
}
