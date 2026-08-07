using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class DockSpace : BaseUIElement<DockSpace>
{
    public DockSpace(string name, uint dockSpaceId, ImGuiDockNodeFlags flags = ImGuiDockNodeFlags.None) : base(name)
    {
        Data = new DockSpaceData
        {
            Name        = name,
            DockSpaceId = dockSpaceId,
            Flags       = flags
        };
    }
}
