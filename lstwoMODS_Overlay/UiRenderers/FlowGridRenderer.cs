using System;
using System.Collections.Generic;
using System.Numerics;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class FlowGridRenderer : UIRenderer
{
    private float _minCellWidth;
    private float _maxCellWidth;
    private List<BaseUIElementData> _children;
    private List<BaseUIElementData> _tail;

    public FlowGridRenderer(BaseUIElementData data) : base(data)
    {
        var d = (FlowGridData)data;
        _minCellWidth = d.MinCellWidth;
        _maxCellWidth = d.MaxCellWidth;
        _children = d.Children;
        _tail     = d.LineChildren;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (FlowGridData)data; Data = d; Name = d.Name;
        _minCellWidth = d.MinCellWidth;
        _maxCellWidth = d.MaxCellWidth;
        if (d.Children?.Count > 0) _children = d.Children;
        if (d.LineChildren?.Count > 0) _tail = d.LineChildren;
    }

    public override void Render()
    {
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var avail = ImGui.GetContentRegionAvail().X;
        var minW = Math.Max(50f, _minCellWidth);
        var columns = Math.Max(1, (int)((avail + spacing) / (minW + spacing)));
        var cellWidth = (avail - spacing * (columns - 1)) / columns;
        if (_maxCellWidth > 0 && cellWidth > _maxCellWidth) cellWidth = _maxCellWidth;

        var col = 0;
        foreach (var child in _children)
        {
            if (!child.Enabled) continue; // hidden children must not occupy grid slots

            if (col % columns != 0) ImGui.SameLine();
            try
            {
                ChildWindowRenderer.PendingCellWidth = cellWidth;
                Window.RenderSingleElement(child);
            }
            finally
            {
                ChildWindowRenderer.PendingCellWidth = null;
            }
            col++;
        }

        // Tail: stretch the optional tail element across the trailing empty space of the last
        // partial row (e.g. a drop zone), matching the last cell's height so it reads as part of
        // that row. Only when the last row isn't full  a full row has no trailing space.
        var tail = _tail is { Count: > 0 } && _tail[0].Enabled ? _tail[0] : null;
        if (tail != null && col > 0 && col % columns != 0)
        {
            var rowHeight = ImGui.GetItemRectSize().Y; // the last cell just rendered
            ImGui.SameLine();
            var remaining = ImGui.GetContentRegionAvail().X;
            if (remaining > 4f && rowHeight > 0f)
            {
                try
                {
                    InvisibleButtonRenderer.PendingSize = new Vector2(remaining, rowHeight);
                    Window.RenderSingleElement(tail);
                }
                finally
                {
                    InvisibleButtonRenderer.PendingSize = null;
                }
            }
        }
    }

    public override BaseUIElementData? GetNewState() => null;
}
