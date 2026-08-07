using System.Collections.Generic;
using System.Numerics;
using Hexa.NET.ImPlot3D;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class Plot3DPanelRenderer : UIRenderer
{
    private string _title;
    private float _sizeX, _sizeY;
    private ImPlot3DFlags _flags;
    private string _xLabel, _yLabel, _zLabel;
    private ImPlot3DAxisFlags _xFlags, _yFlags, _zFlags;
    private List<BaseUIElementData> _children;

    public Plot3DPanelRenderer(BaseUIElementData data) : base(data) { CopyFrom((Plot3DPanelData)data); }

    private void CopyFrom(Plot3DPanelData d)
    {
        _title    = d.Title;
        _sizeX    = d.SizeX;
        _sizeY    = d.SizeY;
        _flags    = (ImPlot3DFlags)(int)d.Flags;
        _xLabel   = d.XLabel;
        _yLabel   = d.YLabel;
        _zLabel   = d.ZLabel;
        _xFlags   = (ImPlot3DAxisFlags)(int)d.XFlags;
        _yFlags   = (ImPlot3DAxisFlags)(int)d.YFlags;
        _zFlags   = (ImPlot3DAxisFlags)(int)d.ZFlags;
        _children = d.Children;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (Plot3DPanelData)data;
        var prev = _children;
        Data = d;
        Name = d.Name;
        CopyFrom(d);
        if (!(d.Children?.Count > 0)) _children = prev;
    }

    public override void Render()
    {
        if (!ImPlot3D.BeginPlot(_title, new Vector2(_sizeX, _sizeY), _flags)) return;

        if (_xLabel != null || _xFlags != 0)
            ImPlot3D.SetupAxis(ImAxis3D.X, _xLabel, _xFlags);
        if (_yLabel != null || _yFlags != 0)
            ImPlot3D.SetupAxis(ImAxis3D.Y, _yLabel, _yFlags);
        if (_zLabel != null || _zFlags != 0)
            ImPlot3D.SetupAxis(ImAxis3D.Z, _zLabel, _zFlags);

        foreach (var child in _children)
            Window.RenderSingleElement(child);

        ImPlot3D.EndPlot();
    }

    public override BaseUIElementData? GetNewState() => null;
}
