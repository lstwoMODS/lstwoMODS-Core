using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class ArrowButtonRenderer : UIRenderer
{
    private bool _pressedThisFrame;
    private ImGuiDir _dir;

    public ArrowButtonRenderer(BaseUIElementData data) : base(data)
    {
        _dir = (ImGuiDir)(int)((ArrowButtonData)data).Dir;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (ArrowButtonData)data; Data = d; Name = d.Name; _dir = (ImGuiDir)(int)d.Dir;
    }

    public override void Render()
    {
        _pressedThisFrame |= ImGui.ArrowButton(Data.Name, _dir);
    }

    public override BaseUIElementData? GetNewState()
    {
        if (!_pressedThisFrame) return null;
        _pressedThisFrame = false;
        return new ArrowButtonData { Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled, Dir = ((ArrowButtonData)Data).Dir, Pressed = true };
    }
}
