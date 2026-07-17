using System.Numerics;
using Hexa.NET.ImPlot;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class PlotDragLineRenderer : UIRenderer
{
    private int _dragId;
    private bool _vertical;
    private double _value;
    private Vector4 _color;
    private float _thickness;
    private ImPlotDragToolFlags _flags;
    private bool _changedThisFrame;

    public PlotDragLineRenderer(BaseUIElementData data) : base(data) { CopyFrom((PlotDragLineData)data); }

    private void CopyFrom(PlotDragLineData d)
    {
        _dragId    = d.DragId;
        _vertical  = d.Vertical;
        _value     = d.Value;
        _color     = new Vector4(d.ColorR, d.ColorG, d.ColorB, d.ColorA);
        _thickness = d.Thickness;
        _flags     = (ImPlotDragToolFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (PlotDragLineData)data;
        Data = d;
        Name = d.Name;
        CopyFrom(d);
    }

    public override void Render()
    {
        bool changed;
        if (_vertical)
            changed = ImPlot.DragLineX(_dragId, ref _value, _color, _thickness, _flags);
        else
            changed = ImPlot.DragLineY(_dragId, ref _value, _color, _thickness, _flags);

        if (changed) _changedThisFrame = true;
    }

    public override BaseUIElementData? GetNewState()
    {
        if (!_changedThisFrame) return null;
        _changedThisFrame = false;
        var d = (PlotDragLineData)Data;
        return new PlotDragLineData
        {
            Id        = Data.Id,
            Name      = Data.Name,
            Enabled   = Data.Enabled,
            DragId    = _dragId,
            Vertical  = _vertical,
            Value     = _value,
            ColorR    = _color.X,
            ColorG    = _color.Y,
            ColorB    = _color.Z,
            ColorA    = _color.W,
            Thickness = _thickness,
            Flags     = d.Flags
        };
    }
}
