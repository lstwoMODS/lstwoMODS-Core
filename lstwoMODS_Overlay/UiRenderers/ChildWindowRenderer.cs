using System.Collections.Generic;
using System.Numerics;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class ChildWindowRenderer : UIRenderer
{
    /// <summary>Set by <see cref="FlowGridRenderer"/> just before rendering a grid child:
    /// the cell width this child window should fill instead of its own SizeX. Consumed
    /// (cleared) by the first ChildWindow that renders.</summary>
    internal static float? PendingCellWidth;

    private float _sizeX, _sizeY, _reserveFooterLines;
    private ImGuiChildFlags _childFlags;
    private ImGuiWindowFlags _windowFlags;
    private List<BaseUIElementData> _children;

    public ChildWindowRenderer(BaseUIElementData data) : base(data)
    {
        var d = (ChildWindowData)data;
        _sizeX = d.SizeX; _sizeY = d.SizeY; _reserveFooterLines = d.ReserveFooterLines;
        _childFlags = (ImGuiChildFlags)(int)d.ChildFlags;
        _windowFlags = (ImGuiWindowFlags)(int)d.WindowFlags;
        _children = d.Children;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (ChildWindowData)data; Data = d; Name = d.Name;
        _sizeX = d.SizeX; _sizeY = d.SizeY; _reserveFooterLines = d.ReserveFooterLines;
        _childFlags = (ImGuiChildFlags)(int)d.ChildFlags;
        _windowFlags = (ImGuiWindowFlags)(int)d.WindowFlags;
        if (d.Children?.Count > 0) _children = d.Children;
    }

    // A "scroll to bottom" (ScrollHereY >= 1) can't be satisfied in a single frame: the child's
    // content streams in from the mod over the IPC boundary and its measured height (GetScrollMaxY)
    // is momentarily 0 on the frames right after the request. A one-shot SetScrollHereY is applied
    // on the *next* frame and clamps to the top whenever that frame reads maxY==0, so it never
    // sticks. Instead we latch a short pin and re-assert SetScrollY(GetScrollMaxY()) every frame
    // until the content settles, which lands on whichever frame actually has the content.
    private int _pinBottomFrames;

    public override void Render()
    {
        // Footer reserve: fill the remaining height minus N widget rows, so siblings
        // below the child (buttons etc.) always sit at the bottom of the parent.
        var sizeY = _reserveFooterLines > 0
            ? -(_reserveFooterLines * ImGui.GetFrameHeightWithSpacing())
            : _sizeY;
        var sizeX = _sizeX;
        if (PendingCellWidth.HasValue)
        {
            sizeX = PendingCellWidth.Value;
            PendingCellWidth = null;
        }
        if (ImGui.BeginChild(Data.Name, new Vector2(sizeX, sizeY), _childFlags, _windowFlags))
        {
            var cd = (ChildWindowData)Data;

            if (cd.ScrollToY.HasValue)   { ImGui.SetScrollY(cd.ScrollToY.Value);      cd.ScrollToY = null; }
            if (cd.ScrollToX.HasValue)   { ImGui.SetScrollX(cd.ScrollToX.Value);      cd.ScrollToX = null; }

            foreach (var child in _children)
                Window.RenderSingleElement(child);

            if (cd.ScrollHereY.HasValue)
            {
                if      (cd.ScrollHereY.Value >= 1f) _pinBottomFrames = 6;
                else if (cd.ScrollHereY.Value <= 0f) ImGui.SetScrollY(0f);
                else                                 ImGui.SetScrollHereY(cd.ScrollHereY.Value);
                cd.ScrollHereY = null;
            }

            if (_pinBottomFrames > 0)
            {
                _pinBottomFrames--;
                ImGui.SetScrollY(ImGui.GetScrollMaxY());
            }

            if (cd.ScrollHereX.HasValue)
            {
                if (cd.ScrollHereX.Value <= 0f) ImGui.SetScrollX(0f);
                else                            ImGui.SetScrollHereX(cd.ScrollHereX.Value);
                cd.ScrollHereX = null;
            }
        }
        ImGui.EndChild();
    }

    public override BaseUIElementData? GetNewState() => null;
}
