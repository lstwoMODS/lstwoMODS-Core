using Hexa.NET.ImPlot3D;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class Plot3DScatterSeriesRenderer : UIRenderer
{
    private float[] _xs, _ys, _zs;
    private ImPlot3DScatterFlags _flags;

    public Plot3DScatterSeriesRenderer(BaseUIElementData data) : base(data) { CopyFrom((Plot3DScatterSeriesData)data); }

    private void CopyFrom(Plot3DScatterSeriesData d)
    {
        _xs    = d.XValues;
        _ys    = d.YValues;
        _zs    = d.ZValues;
        _flags = (ImPlot3DScatterFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (Plot3DScatterSeriesData)data;
        Data = d;
        Name = d.Name;
        CopyFrom(d);
    }

    public override void Render()
    {
        if (_xs == null || _ys == null || _zs == null || _xs.Length == 0) return;
        ImPlot3D.PlotScatter(Data.Name, ref _xs[0], ref _ys[0], ref _zs[0], _xs.Length, _flags);
    }

    public override BaseUIElementData? GetNewState() => null;
}
