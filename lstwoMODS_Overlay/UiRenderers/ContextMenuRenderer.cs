using System.Collections.Generic;
using System.Numerics;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class ContextMenuRenderer : UIRenderer
{
    private bool _onItem;
    private ImGuiPopupFlags _popupFlags;
    private List<BaseUIElementData> _trigger;
    private List<BaseUIElementData> _items;

    public ContextMenuRenderer(BaseUIElementData data) : base(data) { CopyFrom((ContextMenuData)data); }
    private void CopyFrom(ContextMenuData d)
    {
        // Name must be set on the first frame too: BeginPopupContextItem uses it as the popup
        // id, and a null str_id falls back to the last item's ID (asserts id != 0 when that
        // item has none). The base ctor doesn't copy Name, so do it here as well as in ApplyState.
        Name        = d.Name;
        _onItem     = d.OnItem;
        _popupFlags = (ImGuiPopupFlags)(int)d.PopupFlags;
        _trigger    = d.Trigger;
        _items      = d.Items;
    }
    public override void ApplyState(BaseUIElementData data) { var d=(ContextMenuData)data; Data=d; Name=d.Name; CopyFrom(d); }

    public override void Render()
    {
        if (!_onItem)
        {
            // Whole-window context menu.
            foreach (var child in _trigger)
                Window.RenderSingleElement(child);

            if (ImGui.BeginPopupContextWindow(Name, _popupFlags))
            {
                RenderItems();
                ImGui.EndPopup();
            }
            return;
        }

        ImGui.BeginGroup();
        foreach (var child in _trigger)
            Window.RenderSingleElement(child);
        ImGui.EndGroup();

        var button = (ImGuiMouseButton)((int)_popupFlags & (int)ImGuiPopupFlags.MouseButtonMask);
        if (ImGui.IsMouseReleased(button)
            && ImGui.IsMouseHoveringRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax())
            && !ImGui.IsPopupOpen(Name))
            ImGui.OpenPopup(Name);

        if (ImGui.BeginPopup(Name))
        {
            RenderItems();
            ImGui.EndPopup();
        }
    }

    private void RenderItems()
    {
        foreach (var item in _items)
            Window.RenderSingleElement(item);
    }

    public override BaseUIElementData? GetNewState() => null;
}
