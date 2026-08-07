using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class SmallButtonRenderer : UIRenderer
{
    private bool _pressedThisFrame;

    public SmallButtonRenderer(BaseUIElementData data) : base(data) { }

    public override void ApplyState(BaseUIElementData data) { Data = data; Name = data.Name; }

    public override void Render()
    {
        _pressedThisFrame |= ImGui.SmallButton(Data.Name);
    }

    public override BaseUIElementData? GetNewState()
    {
        if (!_pressedThisFrame) return null;
        _pressedThisFrame = false;
        return new SmallButtonData { Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled, Pressed = true };
    }
}
