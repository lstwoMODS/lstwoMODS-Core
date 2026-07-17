using Hexa.NET.ImPlot3D;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class Plot3DSurfaceRenderer : UIRenderer
{
    private float[] _xs, _ys, _zs;
    private int _rows, _cols;
    private ImPlot3DSurfaceFlags _flags;

    public Plot3DSurfaceRenderer(BaseUIElementData data) : base(data) { CopyFrom((Plot3DSurfaceData)data); }

    private void CopyFrom(Plot3DSurfaceData d)
    {
        _xs    = d.XValues;
        _ys    = d.YValues;
        _zs    = d.ZValues;
        _rows  = d.Rows;
        _cols  = d.Cols;
        _flags = (ImPlot3DSurfaceFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (Plot3DSurfaceData)data;
        Data = d;
        Name = d.Name;
        CopyFrom(d);
    }

    public override void Render()
    {
        if (_xs == null || _ys == null || _zs == null || _xs.Length == 0) return;
        ImPlot3D.PlotSurface(Data.Name, ref _xs[0], ref _ys[0], ref _zs[0], _rows, _cols, _flags);
    }

    public override BaseUIElementData? GetNewState() => null;
}
