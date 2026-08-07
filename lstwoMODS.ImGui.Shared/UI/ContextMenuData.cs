using System.Collections.Generic;
namespace lstwoMODS.ImGui.Shared.UI
{
    public class ContextMenuData : BaseUIElementData
    {
        /// <summary>true = BeginPopupContextItem (previous item), false = BeginPopupContextWindow (whole window).</summary>
        public bool   OnItem     { get; set; } = true;
        public ImGuiPopupFlags PopupFlags { get; set; } = ImGuiPopupFlags.MouseButtonRight;
        /// <summary>Content that receives the right-click; the context menu appears on this.</summary>
        public List<BaseUIElementData> Trigger { get; set; } = new List<BaseUIElementData>();
        /// <summary>Items rendered inside the popup (MenuItems, Menus, Separators, etc.).</summary>
        public List<BaseUIElementData> Items   { get; set; } = new List<BaseUIElementData>();
    }
}
