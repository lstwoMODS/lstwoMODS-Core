using System.Numerics;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class ProgressBarRenderer : UIRenderer
{
    private float  _value;
    private float  _sizeX, _sizeY;
    private string _overlay;

    public ProgressBarRenderer(BaseUIElementData data) : base(data)
    {
        var d = (ProgressBarData)data;
        _value = d.Value; _sizeX = d.SizeX; _sizeY = d.SizeY; _overlay = d.Overlay;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (ProgressBarData)data;
        Data = d; Name = d.Name;
        _value = d.Value; _sizeX = d.SizeX; _sizeY = d.SizeY; _overlay = d.Overlay;
    }

    public override void Render()
    {
        ImGui.ProgressBar(_value, new Vector2(_sizeX, _sizeY), _overlay);
    }

    public override BaseUIElementData? GetNewState() => null; // display-only, no user-driven state change
}
