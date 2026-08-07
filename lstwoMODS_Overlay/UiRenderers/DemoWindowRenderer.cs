using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class DemoWindowRenderer : UIRenderer
{
    public DemoWindowRenderer(BaseUIElementData data) : base(data)
    {
    }

    public override void ApplyState(BaseUIElementData data)
    {
        Data = data;
        Name = data.Name;
    }

    public override void Render()
    {
        ImGui.ShowDemoWindow();
    }

    public override BaseUIElementData? GetNewState()
    {
        return null;
    }
}