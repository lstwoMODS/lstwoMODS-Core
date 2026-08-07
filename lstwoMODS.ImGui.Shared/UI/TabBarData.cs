using System.Collections.Generic;

namespace lstwoMODS.ImGui.Shared.UI
{
    public class TabBarData : BaseUIElementData
    {
        public ImGuiTabBarFlags Flags { get; set; } = ImGuiTabBarFlags.None;
        public List<BaseUIElementData> Children { get; set; } = new List<BaseUIElementData>();
    }

    public class TabItemData : BaseUIElementData
    {
        public string Label     { get; set; } = "";
        public bool   Open      { get; set; } = true;   // false = tab was closed via X
        public bool   ShowClose { get; set; } = false;  // true = show X button
        public ImGuiTabItemFlags Flags { get; set; } = ImGuiTabItemFlags.None;
        public List<BaseUIElementData> Children { get; set; } = new List<BaseUIElementData>();
    }
}
