using System.Collections.Generic;
using System.Numerics;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class MenuItemRenderer : UIRenderer
{
    private bool   _selected;
    private bool   _checkable;
    private string _shortcut;
    private bool   _itemEnabled;
    private bool   _clickedThisFrame;

    public MenuItemRenderer(BaseUIElementData data) : base(data) { Name = data.Name; CopyFrom((MenuItemData)data); }
    private void CopyFrom(MenuItemData d) { _selected = d.Selected; _checkable = d.Checkable; _shortcut = d.Shortcut; _itemEnabled = d.ItemEnabled; }
    public override void ApplyState(BaseUIElementData data) { var d=(MenuItemData)data; Data=d; Name=d.Name; CopyFrom(d); }

    public override void Render()
    {
        // Checkable: show/toggle a checkmark reflecting Selected. Otherwise a plain action item
        // (the value overload with selected=false draws no checkmark and never toggles).
        var clicked = _checkable
            ? ImGui.MenuItem(Data.Name, _shortcut, ref _selected, _itemEnabled)
            : ImGui.MenuItem(Data.Name, _shortcut, false, _itemEnabled);
        if (clicked) _clickedThisFrame = true;
    }

    public override BaseUIElementData? GetNewState()
    {
        var d = (MenuItemData)Data;
        if (!_clickedThisFrame && _selected == d.Selected) return null;
        var clicked = _clickedThisFrame;
        _clickedThisFrame = false;
        d.Selected = _selected;
        // Echo Checkable back so the mod side keeps it (base.ApplyReceivedData swaps in this object).
        return new MenuItemData { Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled, Selected = _selected, Checkable = _checkable, Shortcut = _shortcut, ItemEnabled = _itemEnabled, Clicked = clicked };
    }
}
