using System.Collections.Generic;
namespace lstwoMODS.ImGui.Shared.UI
{
    public class PopupData : BaseUIElementData
    {
        public bool              IsOpen   { get; set; } = false;
        public ImGuiWindowFlags  Flags    { get; set; } = ImGuiWindowFlags.None;
        public List<BaseUIElementData> Children { get; set; } = new List<BaseUIElementData>();
    }
}
