using System.Collections.Generic;
namespace lstwoMODS.ImGui.Shared.UI
{
    public class MenuItemData : BaseUIElementData
    {
        public string Shortcut    { get; set; } = null;
        public bool   Selected    { get; set; } = false;
        // true = show a checkmark reflecting Selected and toggle it on click (a stateful item);
        // false = a plain action item with no checkmark column. Set true when a selected state
        // is assigned (see MenuItem.WithSelected / the Selected setter).
        public bool   Checkable   { get; set; } = false;
        public bool   ItemEnabled { get; set; } = true;   // false = grayed out, non-clickable
        public bool   Clicked     { get; set; } = false;  // set by overlay when clicked
    }

    public class MenuData : BaseUIElementData
    {
        public string Label       { get; set; } = "";
        public bool   MenuEnabled { get; set; } = true;
        public List<BaseUIElementData> Children { get; set; } = new List<BaseUIElementData>();
    }

    public class MenuBarData : BaseUIElementData
    {
        public List<BaseUIElementData> Children { get; set; } = new List<BaseUIElementData>();
    }

    /// <summary>ImGui.BeginMainMenuBar  full-width menu bar at the top of the display.</summary>
    public class MainMenuBarData : BaseUIElementData
    {
        public List<BaseUIElementData> Children { get; set; } = new List<BaseUIElementData>();
    }

    /// <summary>Calls ImGui.CloseCurrentPopup()  place inside a popup/modal to close it from a button callback.</summary>
    public class ClosePopupData : BaseUIElementData { }
}
