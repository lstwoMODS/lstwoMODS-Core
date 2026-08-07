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
    private List<BaseUIElementData> _children;

    private bool   _hasNewDrop;
    private string _droppedType;
    private string _droppedPayload;
    private bool   _droppedAfter;

    public DragTargetRenderer(BaseUIElementData data) : base(data)
    {
        var d       = (DragTargetData)data;
        _acceptTypes   = d.AcceptTypes;
        _insertBetween = d.InsertBetween;
        _children      = d.Children;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d        = (DragTargetData)data;
        Data           = d;
        Name           = d.Name;
        _acceptTypes   = d.AcceptTypes;
        _insertBetween = d.InsertBetween;
        if (d.Children?.Count > 0) _children = d.Children;
    }

    public override unsafe void Render()
    {
        ImGui.BeginGroup();
        foreach (var child in _children)
            Window.RenderSingleElement(child);
        ImGui.EndGroup();

        if (!ImGui.BeginDragDropTarget()) return;

        // Insert-between: draw a vertical line at the edge of whichever half the cursor is over
        // (before / after this element) instead of the default whole-element highlight box, so a
        // drop lands in the gap between items rather than "on" one.
        var rectMin = ImGui.GetItemRectMin();
        var rectMax = ImGui.GetItemRectMax();
        var after   = ImGui.GetMousePos().X >= (rectMin.X + rectMax.X) * 0.5f;
        // Centred in the spacing gap, not flush against this element, see CollapsingHeaderRenderer.
        var gap     = ImGui.GetStyle().ItemSpacing.X * 0.5f;

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
                var x = after ? rectMax.X + gap : rectMin.X - gap;
                ImGui.GetWindowDrawList().AddLine(
                    new Vector2(x, rectMin.Y), new Vector2(x, rectMax.Y),
                    ImGui.GetColorU32(ImGuiCol.DragDropTarget), 3f);
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
            Children       = _children,
            DroppedType    = _droppedType,
            DroppedPayload = _droppedPayload,
            DroppedAfter   = _droppedAfter,
        };
    }
}
