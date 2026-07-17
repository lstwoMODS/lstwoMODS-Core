using System.Numerics;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class DockSpaceRenderer : UIRenderer
{
    private uint _dockSpaceId;
    private ImGuiDockNodeFlags _flags;

    public DockSpaceRenderer(BaseUIElementData data) : base(data)
    {
        var d      = (DockSpaceData)data;
        _dockSpaceId = d.DockSpaceId;
        _flags       = (ImGuiDockNodeFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d      = (DockSpaceData)data;
        Data         = d;
        _dockSpaceId = d.DockSpaceId;
        _flags       = (ImGuiDockNodeFlags)(int)d.Flags;
    }

    public override void Render()
    {
        ImGui.DockSpace(_dockSpaceId, new Vector2(0, 0), _flags);
    }

    public override BaseUIElementData? GetNewState() => null;
}
