using Hexa.NET.ImPlot3D;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class Plot3DLineSeriesRenderer : UIRenderer
{
    private float[] _xs, _ys, _zs;
    private ImPlot3DLineFlags _flags;

    public Plot3DLineSeriesRenderer(BaseUIElementData data) : base(data) { CopyFrom((Plot3DLineSeriesData)data); }

    private void CopyFrom(Plot3DLineSeriesData d)
    {
        _xs    = d.XValues;
        _ys    = d.YValues;
        _zs    = d.ZValues;
        _flags = (ImPlot3DLineFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (Plot3DLineSeriesData)data;
        Data = d;
        Name = d.Name;
        CopyFrom(d);
    }

    public override void Render()
    {
        if (_xs == null || _ys == null || _zs == null || _xs.Length == 0) return;
        ImPlot3D.PlotLine(Data.Name, ref _xs[0], ref _ys[0], ref _zs[0], _xs.Length, _flags);
    }

    public override BaseUIElementData? GetNewState() => null;
}
