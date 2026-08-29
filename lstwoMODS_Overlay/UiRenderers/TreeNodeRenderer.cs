using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class TreeNodeRenderer : UIRenderer
{
    private string _label;
    private ImGuiTreeNodeFlags _flags;
    private List<BaseUIElementData> _children;
    private List<BaseUIElementData> _lineChildren;
    private bool   _pinLineChildrenEnd;
    private string _lineTag;
    private string _lineTagTooltip;

    /// <summary>Width of the pinned line children, measured while rendering and used to
    /// position them the next frame (classic ImGui right-align two-pass).</summary>
    private float _pinnedWidth = 40f;

    private string   _dragPayloadType;
    private string   _dragPayloadData;
    private string   _dragDisplayLabel;
    private string[] _acceptDropTypes;

    private bool   _hasNewDrop;
    private string _droppedType;
    private string _droppedPayload;
    private bool   _droppedBelow;

    public TreeNodeRenderer(BaseUIElementData data) : base(data)
    {
        var d = (TreeNodeData)data;
        _label = d.Label; _flags = (ImGuiTreeNodeFlags)(int)d.Flags; _children = d.Children;
        _lineChildren = d.LineChildren;
        _pinLineChildrenEnd = d.PinLineChildrenEnd;
        _lineTag          = d.LineTag;
        _lineTagTooltip   = d.LineTagTooltip;
        _dragPayloadType  = d.DragPayloadType;
        _dragPayloadData  = d.DragPayloadData;
        _dragDisplayLabel = d.DragDisplayLabel;
        _acceptDropTypes  = d.AcceptDropTypes;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (TreeNodeData)data; Data = d; Name = d.Name;
        _label = d.Label; _flags = (ImGuiTreeNodeFlags)(int)d.Flags; if (d.Children?.Count > 0) _children = d.Children;
        if (d.LineChildren?.Count > 0) _lineChildren = d.LineChildren;
        _pinLineChildrenEnd = d.PinLineChildrenEnd;
        _lineTag          = d.LineTag;
        _lineTagTooltip   = d.LineTagTooltip;
        _dragPayloadType  = d.DragPayloadType;
        _dragPayloadData  = d.DragPayloadData;
        _dragDisplayLabel = d.DragDisplayLabel;
        _acceptDropTypes  = d.AcceptDropTypes;
    }

    public override unsafe bool RenderWidget()
    {
        // Right edge of the line in window-local coords (for SameLine offsets), captured
        // before the node renders while the cursor is still at line start.
        var localRight = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X;

        var open = ImGui.TreeNodeEx(_label, _flags);

        // Drag & drop applies to the node line just rendered: must run before anything
        // else (like the X button) changes ImGui's "last item". SourceNoHoldToOpenOthers kills
        // hold-to-open while dragging, see CollapsingHeaderRenderer for the rationale.
        if (_dragPayloadType != null && ImGui.BeginDragDropSource(ImGuiDragDropFlags.SourceNoHoldToOpenOthers))
        {
            var bytes = Encoding.UTF8.GetBytes(_dragPayloadData ?? "");
            fixed (byte* ptr = bytes)
                ImGui.SetDragDropPayload(_dragPayloadType, ptr, (uint)bytes.Length);

            var dragText = _dragDisplayLabel ?? _label;
            var idSuffix = dragText.IndexOf("##", StringComparison.Ordinal);
            if (idSuffix >= 0) dragText = dragText.Substring(0, idSuffix);
            ImGui.Text(dragText);
            ImGui.EndDragDropSource();
        }

        if (_acceptDropTypes is { Length: > 0 } && ImGui.BeginDragDropTarget())
        {
            // Insert-between semantics, see CollapsingHeaderRenderer for the rationale.
            var rectMin = ImGui.GetItemRectMin();
            var rectMax = ImGui.GetItemRectMax();
            var below   = ImGui.GetMousePos().Y >= (rectMin.Y + rectMax.Y) * 0.5f;
            var gap     = ImGui.GetStyle().ItemSpacing.Y * 0.5f;

            foreach (var type in _acceptDropTypes)
            {
                var payload = ImGui.AcceptDragDropPayload(type,
                    ImGuiDragDropFlags.AcceptBeforeDelivery | ImGuiDragDropFlags.AcceptNoDrawDefaultRect);
                if (payload.IsNull) continue;
                if (payload.Data == null || payload.DataSize <= 0) continue;

                var y = below ? rectMax.Y + gap : rectMin.Y - gap;
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

        // Line extras live on the node's line, after the label  unlike regular children
        // they render whether or not the node is open. Must run after the drag/drop handling
        // above, which needs the node line to still be ImGui's "last item".
        if (_lineChildren is { Count: > 0 } || !string.IsNullOrEmpty(_lineTag))
            RenderLineExtras(localRight);

        return open;
    }

    /// <summary>The line tag and line children. Flow layout puts children right after the
    /// label; pinned layout anchors them to the right edge with the tag ellipsized into
    /// whatever gap remains, so the line can never overflow its container. Everything is
    /// positioned via SameLine offsets  raw SetCursorScreenPos breaks ImGui's line
    /// bookkeeping and asserts when the node is the last item in its window.</summary>
    private void RenderLineExtras(float localRight)
    {
        if (!_pinLineChildrenEnd)
        {
            if (_lineChildren is { Count: > 0 })
            {
                for (var i = 0; i < _lineChildren.Count; i++)
                {
                    ImGui.SameLine(0f, i == 0 ? 12f : -1f);
                    Window.RenderSingleElement(_lineChildren[i]);
                }
            }
            if (!string.IsNullOrEmpty(_lineTag))
            {
                ImGui.SameLine(0f, 12f);
                ImGui.TextDisabled(_lineTag);
                TagTooltip();
            }
            return;
        }

        var style = ImGui.GetStyle();
        // Label end in window-local coords (SameLine offsets are window-local).
        var windowLeft    = ImGui.GetWindowPos().X - ImGui.GetScrollX();
        var labelEndLocal = ImGui.GetItemRectMax().X - windowLeft;

        var pinStart = localRight - _pinnedWidth;
        var minPinStart = labelEndLocal + style.ItemSpacing.X;
        if (pinStart < minPinStart) pinStart = minPinStart;

        if (!string.IsNullOrEmpty(_lineTag))
        {
            var tagX  = labelEndLocal + 12f;
            var avail = pinStart - style.ItemSpacing.X - tagX;
            if (avail > 14f)
            {
                var text = FitWithEllipsis(_lineTag, avail);
                if (text.Length > 0)
                {
                    ImGui.SameLine(tagX, 0f);
                    ImGui.TextDisabled(text);
                    TagTooltip();
                }
            }
        }

        if (_lineChildren is { Count: > 0 })
        {
            ImGui.SameLine(pinStart, 0f);
            var startX = ImGui.GetCursorScreenPos().X;
            for (var i = 0; i < _lineChildren.Count; i++)
            {
                if (i > 0) ImGui.SameLine();
                Window.RenderSingleElement(_lineChildren[i]);
            }
            _pinnedWidth = ImGui.GetItemRectMax().X - startX;
        }
    }

    private void TagTooltip()
    {
        if (!string.IsNullOrEmpty(_lineTagTooltip))
            Window.RenderTooltip(_lineTagTooltip, ImGuiHoveredFlags.DelayNormal);
    }

    private static string FitWithEllipsis(string text, float avail)
    {
        if (ImGui.CalcTextSize(text).X <= avail) return text;
        const string ellipsis = "…";
        var lo = 0;
        var hi = text.Length;
        while (lo < hi)
        {
            var mid = (lo + hi + 1) / 2;
            if (ImGui.CalcTextSize(text.Substring(0, mid) + ellipsis).X <= avail) lo = mid;
            else hi = mid - 1;
        }
        return lo == 0 ? "" : text.Substring(0, lo) + ellipsis;
    }

    public override void RenderChildren()
    {
        foreach (var child in _children)
            Window.RenderSingleElement(child);
        ImGui.TreePop();
    }

    public override void Render()
    {
        if (RenderWidget()) RenderChildren();
    }

    public override BaseUIElementData? GetNewState()
    {
        // Tree open state is managed by ImGui internally; only drops need to reach the
        // mod side. Line children report their own state through their own renderers.
        if (!_hasNewDrop) return null;

        var d = (TreeNodeData)Data;
        var state = new TreeNodeData
        {
            Id           = Data.Id,
            Name         = Data.Name,
            Enabled      = Data.Enabled,
            Label        = _label,
            Flags        = d.Flags,
            Children     = _children,
            LineChildren = _lineChildren,
            PinLineChildrenEnd = _pinLineChildrenEnd,
            LineTag        = _lineTag,
            LineTagTooltip = _lineTagTooltip,

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
