using System.Collections.Generic;
using System.Linq;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

/// <summary>
/// Renders a <see cref="PinRowData"/>: lead children flow from the left, trailing children are
/// right-aligned to the row's right edge on the same line when they fit, otherwise wrapped onto
/// a fresh right-aligned line. Right-alignment uses the classic ImGui two-pass trick  the trail
/// width is measured while rendering and used to place it the next frame  and only SameLine /
/// window-local cursor offsets, so it never trips ImGui's line bookkeeping.
/// </summary>
public class PinRowRenderer : UIRenderer
{
    private List<BaseUIElementData> _lead;
    private List<BaseUIElementData> _trail;

    /// <summary>Measured width of the pinned trail, used to right-align it next frame.</summary>
    private float _trailWidth = 60f;

    public PinRowRenderer(BaseUIElementData data) : base(data)
    {
        var d = (PinRowData)data;
        _lead  = d.Children;
        _trail = d.LineChildren;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (PinRowData)data;
        Data = d; Name = d.Name;
        if (d.Children?.Count > 0)     _lead  = d.Children;
        if (d.LineChildren?.Count > 0) _trail = d.LineChildren;
    }

    public override void Render()
    {
        var lead  = _lead?.Where(c => c.Enabled).ToList()  ?? new List<BaseUIElementData>();
        var trail = _trail?.Where(c => c.Enabled).ToList() ?? new List<BaseUIElementData>();

        var style      = ImGui.GetStyle();
        var rowStartX  = ImGui.GetCursorPosX();
        var windowLeft = ImGui.GetWindowPos().X - ImGui.GetScrollX();

        var availRight  = rowStartX + ImGui.GetContentRegionAvail().X;
        var paddedRight = ImGui.GetWindowWidth() - style.WindowPadding.X * 2;
        var rightEdge   = availRight < paddedRight ? availRight : paddedRight;

        // ── Lead: rendered in order; the caller supplies its own SameLine elements. ──
        var leadEndLocal = rowStartX;
        foreach (var c in lead)
            Window.RenderSingleElement(c);
        if (lead.Count > 0)
            leadEndLocal = ImGui.GetItemRectMax().X - windowLeft;

        if (trail.Count == 0) return;

        // ── Trail: right-pinned on the same line if it fits, else wrapped below. ──
        var pinStart    = rightEdge - _trailWidth;
        var minSameLine = leadEndLocal + style.ItemSpacing.X;

        if (lead.Count == 0)
        {
            ImGui.SetCursorPosX(pinStart < rowStartX ? rowStartX : pinStart);
        }
        else if (pinStart >= minSameLine)
        {
            ImGui.SameLine(pinStart, 0f);
        }
        else
        {
            var wrapStart = rightEdge - _trailWidth;
            ImGui.SetCursorPosX(wrapStart < rowStartX ? rowStartX : wrapStart);
        }

        var startScreenX = ImGui.GetCursorScreenPos().X;
        for (var i = 0; i < trail.Count; i++)
        {
            if (i > 0) ImGui.SameLine();
            Window.RenderSingleElement(trail[i]);
        }
        _trailWidth = ImGui.GetItemRectMax().X - startScreenX;
    }

    public override BaseUIElementData? GetNewState() => null;
}
