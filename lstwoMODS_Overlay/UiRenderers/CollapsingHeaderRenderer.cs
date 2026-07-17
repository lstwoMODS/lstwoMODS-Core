using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class CollapsingHeaderRenderer : UIRenderer
{
    private string _label;
    private bool   _hasClose;
    private bool   _visible;
    private bool   _isOpen;
    private ImGuiTreeNodeFlags _flags;
    private List<BaseUIElementData> _children;

    private string   _dragPayloadType;
    private string   _dragPayloadData;
    private string   _dragDisplayLabel;
    private string[] _acceptDropTypes;

    private bool   _hasNewDrop;
    private string _droppedType;
    private string _droppedPayload;
    private bool   _droppedBelow;

    public CollapsingHeaderRenderer(BaseUIElementData data) : base(data)
    {
        var d = (CollapsingHeaderData)data;
        _label    = d.Label;
        _hasClose = d.HasClose;
        _visible  = d.Visible;
        _isOpen   = d.IsOpen;
        _flags    = (ImGuiTreeNodeFlags)(int)d.Flags;
        _children = d.Children;

        _dragPayloadType  = d.DragPayloadType;
        _dragPayloadData  = d.DragPayloadData;
        _dragDisplayLabel = d.DragDisplayLabel;
        _acceptDropTypes  = d.AcceptDropTypes;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (CollapsingHeaderData)data;
        Data      = d;
        Name      = d.Name;
        _label    = d.Label;
        _hasClose = d.HasClose;
        _visible  = d.Visible;
        _isOpen   = d.IsOpen;
        _flags    = (ImGuiTreeNodeFlags)(int)d.Flags;
        if (d.Children?.Count > 0) _children = d.Children;

        _dragPayloadType  = d.DragPayloadType;
        _dragPayloadData  = d.DragPayloadData;
        _dragDisplayLabel = d.DragDisplayLabel;
        _acceptDropTypes  = d.AcceptDropTypes;
    }

    public override unsafe bool RenderWidget()
    {
        // When hidden by search filter, leave _isOpen untouched so the mod side keeps
        // its last known open state instead of seeing a spurious open→closed transition.
        if (!_visible) return false;

        bool open;
        if (_hasClose)
            open = ImGui.CollapsingHeader(_label, ref _visible, _flags);
        else
            open = ImGui.CollapsingHeader(_label, _flags);

        _isOpen = open;

        // Drag & drop applies to the header item just rendered  the bar only, not the
        // open body. Both must run before anything else changes ImGui's "last item".
        if (_dragPayloadType != null && ImGui.BeginDragDropSource())
        {
            var bytes = Encoding.UTF8.GetBytes(_dragPayloadData ?? "");
            fixed (byte* ptr = bytes)
                ImGui.SetDragDropPayload(_dragPayloadType, ptr, (uint)bytes.Length);

            ImGui.Text(_dragDisplayLabel ?? _label);
            ImGui.EndDragDropSource();
        }

        if (_acceptDropTypes is { Length: > 0 } && ImGui.BeginDragDropTarget())
        {
            // Insert-between semantics: the mouse's half of the header bar decides
            // above/below, and a line is drawn at that edge instead of ImGui's
            // default whole-item highlight  so a drag hovering near the boundary
            // of two headers reads as "drop into the gap between them".
            var rectMin = ImGui.GetItemRectMin();
            var rectMax = ImGui.GetItemRectMax();
            var below   = ImGui.GetMousePos().Y >= (rectMin.Y + rectMax.Y) * 0.5f;

            foreach (var type in _acceptDropTypes)
            {
                var payload = ImGui.AcceptDragDropPayload(type,
                    ImGuiDragDropFlags.AcceptBeforeDelivery | ImGuiDragDropFlags.AcceptNoDrawDefaultRect);
                if (payload.IsNull) continue;
                if (payload.Data == null || payload.DataSize <= 0) continue;

                var y = below ? rectMax.Y : rectMin.Y;
                ImGui.GetWindowDrawList().AddLine(
                    new Vector2(rectMin.X, y), new Vector2(rectMax.X, y),
                    ImGui.GetColorU32(ImGuiCol.DragDropTarget), 3f);

                if (payload.Delivery)
                {
                    _droppedType    = type;
                    _droppedPayload = Encoding.UTF8.GetString((byte*)payload.Data, payload.DataSize);
                    _droppedBelow   = below;
                    _hasNewDrop     = true;
                }
                break;
            }

            ImGui.EndDragDropTarget();
        }

        return open;
    }

    public override void RenderChildren()
    {
        foreach (var child in _children)
            Window.RenderSingleElement(child);
    }

    public override void Render()
    {
        // Not used by the standard dispatch (RenderSingleElement calls RenderWidget +
        // RenderChildren). Kept for parity with the abstract base.
        var open = RenderWidget();
        if (open) RenderChildren();
    }

    public override BaseUIElementData? GetNewState()
    {
        var d = (CollapsingHeaderData)Data;
        var visibleChanged = _hasClose && _visible != d.Visible;
        var openChanged    = _isOpen != d.IsOpen;

        if (!visibleChanged && !openChanged && !_hasNewDrop) return null;

        if (visibleChanged) d.Visible = _visible;
        d.IsOpen = _isOpen;

        var state = new CollapsingHeaderData
        {
            Id       = Data.Id,
            Name     = Data.Name,
            Enabled  = Data.Enabled,
            Label    = _label,
            HasClose = _hasClose,
            Visible  = _visible,
            IsOpen   = _isOpen,
            Flags    = d.Flags,
            Children = _children,

            DragPayloadType  = _dragPayloadType,
            DragPayloadData  = _dragPayloadData,
            DragDisplayLabel = _dragDisplayLabel,
            AcceptDropTypes  = _acceptDropTypes,
        };

        if (_hasNewDrop)
        {
            _hasNewDrop = false;
            state.DroppedType    = _droppedType;
            state.DroppedPayload = _droppedPayload;
            state.DroppedBelow   = _droppedBelow;
            _droppedType    = null;
            _droppedPayload = null;
        }

        return state;
    }
}
