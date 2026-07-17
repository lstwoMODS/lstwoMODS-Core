using System.Collections.Generic;
namespace lstwoMODS.ImGui.Shared.UI
{
    public class ModalData : BaseUIElementData
    {
        public string            Label    { get; set; } = "";
        public bool              IsOpen   { get; set; } = false;

        // Initial window size (0 = auto-fit to content). Applied with ImGuiCond.FirstUseEver,
        // so the modal stays user-resizable and a resize sticks for the session. Required
        // when the content stretches (e.g. a fill-height ChildWindow)  auto-fit can't
        // measure stretchy children.
        public float             SizeX    { get; set; } = 0f;
        public float             SizeY    { get; set; } = 0f;
        public bool              HasClose { get; set; } = true;   // show X button
        public ImGuiWindowFlags  Flags    { get; set; } = ImGuiWindowFlags.None;
        public List<BaseUIElementData> Children { get; set; } = new List<BaseUIElementData>();
    }
}
