using System.Numerics;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class ButtonRenderer : UIRenderer
{
    private bool _pressedThisFrame;

    public ButtonRenderer(BaseUIElementData data) : base(data) { }

    public override void ApplyState(BaseUIElementData data)
    {
        Data = data;
        Name = data.Name;
    }

    public override void Render()
    {
        var d = (ButtonData)Data;
        var slotWidth = RenderContext.SlotWidth;
        var size = slotWidth > 0f
            ? new Vector2(slotWidth, 0)
            : d.UseContentWidth
                ? new Vector2(ImGui.CalcItemWidth(), 0)
                : Vector2.Zero;
        // |= accumulates presses across frames while _awaitingFrameState is true,
        // so a click is never lost if the overlay renders again before GetNewState() is called.
        _pressedThisFrame |= ImGui.Button(Data.Name, size);
    }

    public override BaseUIElementData? GetNewState()
    {
        if (!_pressedThisFrame) return null;
        _pressedThisFrame = false;
        var d = (ButtonData)Data;
        return new ButtonData
        {
            Id             = d.Id,
            Name           = d.Name,
            Enabled        = d.Enabled,
            UseContentWidth = d.UseContentWidth,
            Pressed        = true
        };
    }
}
