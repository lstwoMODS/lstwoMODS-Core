using System.Numerics;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class InvisibleButtonRenderer : UIRenderer
{
    /// <summary>Set by a parent renderer (e.g. <see cref="FlowGridRenderer"/>'s tail) just before
    /// rendering this button to override its size with a value only known at layout time (the
    /// remaining row width). Consumed (cleared) by the first InvisibleButton that renders.</summary>
    internal static Vector2? PendingSize;

    private bool _pressedThisFrame;
    private float _sizeX, _sizeY;

    public InvisibleButtonRenderer(BaseUIElementData data) : base(data)
    {
        var d = (InvisibleButtonData)data; _sizeX = d.SizeX; _sizeY = d.SizeY;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (InvisibleButtonData)data; Data = d; Name = d.Name; _sizeX = d.SizeX; _sizeY = d.SizeY;
    }

    public override void Render()
    {
        var size = new Vector2(_sizeX, _sizeY);
        if (PendingSize.HasValue)
        {
            size = PendingSize.Value;
            PendingSize = null;
        }
        // ImGui asserts on a zero-size invisible button; skip if a caller supplied an empty size.
        if (size.X <= 0f || size.Y <= 0f) return;
        _pressedThisFrame |= ImGui.InvisibleButton(Data.Name, size);
    }

    public override BaseUIElementData? GetNewState()
    {
        if (!_pressedThisFrame) return null;
        _pressedThisFrame = false;
        return new InvisibleButtonData { Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled, SizeX = _sizeX, SizeY = _sizeY, Pressed = true };
    }
}
