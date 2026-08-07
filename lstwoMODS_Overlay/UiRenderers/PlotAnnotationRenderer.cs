using System.Numerics;
using Hexa.NET.ImPlot;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class PlotAnnotationRenderer : UIRenderer
{
    private double _x, _y;
    private float _pixOffX, _pixOffY;
    private string _text;
    private bool _clamp;
    private Vector4 _color;

    public PlotAnnotationRenderer(BaseUIElementData data) : base(data) { CopyFrom((PlotAnnotationData)data); }

    private void CopyFrom(PlotAnnotationData d)
    {
        _x       = d.X;
        _y       = d.Y;
        _pixOffX = d.PixOffX;
        _pixOffY = d.PixOffY;
        _text    = d.Text;
        _clamp   = d.Clamp;
        _color   = new Vector4(d.ColorR, d.ColorG, d.ColorB, d.ColorA);
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (PlotAnnotationData)data;
        Data = d;
        Name = d.Name;
        CopyFrom(d);
    }

    public override void Render()
    {
        ImPlot.Annotation(_x, _y, _color, new Vector2(_pixOffX, _pixOffY), _clamp, _text);
    }

    public override BaseUIElementData? GetNewState() => null;
}
