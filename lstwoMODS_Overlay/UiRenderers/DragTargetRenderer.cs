using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class DragTargetRenderer : UIRenderer
{
    private string[] _acceptTypes;
    private bool     _insertBetween;
    private bool     _insertVertical;
    private List<BaseUIElementData> _children;

    private bool   _hasNewDrop;
    private string _droppedType;
    private string _droppedPayload;
    private bool   _droppedAfter;

    public DragTargetRenderer(BaseUIElementData data) : base(data)
    {
        var d       = (DragTargetData)data;
        _acceptTypes    = d.AcceptTypes;
        _insertBetween  = d.InsertBetween;
        _insertVertical = d.InsertVertical;
        _children       = d.Children;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d        = (DragTargetData)data;
        Data            = d;
        Name            = d.Name;
        _acceptTypes    = d.AcceptTypes;
        _insertBetween  = d.InsertBetween;
        _insertVertical = d.InsertVertical;
        if (d.Children?.Count > 0) _children = d.Children;
    }

    public override unsafe void Render()
    {
        ImGui.BeginGroup();
        foreach (var child in _children)
            Window.RenderSingleElement(child);
        ImGui.EndGroup();

        if (!ImGui.BeginDragDropTarget()) return;

        // Insert-between: draw a line at the edge of whichever half the cursor is over (before /
        // after this element) instead of the default whole-element highlight box, so a drop lands in
        // the gap between items rather than "on" one. The axis follows how the list is laid out: a
        // vertical list needs a horizontal line above or below the row, not one beside it.
        var rectMin = ImGui.GetItemRectMin();
        var rectMax = ImGui.GetItemRectMax();
        var mouse   = ImGui.GetMousePos();
        var style   = ImGui.GetStyle();

        var after = _insertVertical
            ? mouse.Y >= (rectMin.Y + rectMax.Y) * 0.5f
            : mouse.X >= (rectMin.X + rectMax.X) * 0.5f;

        // Centred in the spacing gap, not flush against this element, see CollapsingHeaderRenderer.
        var gap = (_insertVertical ? style.ItemSpacing.Y : style.ItemSpacing.X) * 0.5f;

        foreach (var type in _acceptTypes)
        {
            var flags = _insertBetween
                ? ImGuiDragDropFlags.AcceptBeforeDelivery | ImGuiDragDropFlags.AcceptNoDrawDefaultRect
                : ImGuiDragDropFlags.None;
            var payload = ImGui.AcceptDragDropPayload(type, flags);
            if (payload.IsNull) continue;
            if (payload.Data == null || payload.DataSize <= 0) continue;

            if (_insertBetween)
            {
                var color = ImGui.GetColorU32(ImGuiCol.DragDropTarget);

                if (_insertVertical)
                {
                    var y = after ? rectMax.Y + gap : rectMin.Y - gap;
                    ImGui.GetWindowDrawList().AddLine(
                        new Vector2(rectMin.X, y), new Vector2(rectMax.X, y), color, 3f);
                }
                else
                {
                    var x = after ? rectMax.X + gap : rectMin.X - gap;
                    ImGui.GetWindowDrawList().AddLine(
                        new Vector2(x, rectMin.Y), new Vector2(x, rectMax.Y), color, 3f);
                }
            }

            if (payload.Delivery)
            {
                _droppedType    = type;
                _droppedPayload = Encoding.UTF8.GetString((byte*)payload.Data, payload.DataSize);
                _droppedAfter   = after;
                _hasNewDrop     = true;
            }
            break;
        }

        ImGui.EndDragDropTarget();
    }

    public override BaseUIElementData? GetNewState()
    {
        if (!_hasNewDrop) return null;
        _hasNewDrop = false;

        var d = (DragTargetData)Data;
        return new DragTargetData
        {
            Id             = Data.Id,
            Name           = Data.Name,
            Enabled        = Data.Enabled,
            AcceptTypes    = _acceptTypes,
            InsertBetween  = _insertBetween,
            InsertVertical = _insertVertical,
            Children       = _children,
            DroppedType    = _droppedType,
            DroppedPayload = _droppedPayload,
            DroppedAfter   = _droppedAfter,
        };
    }
}
