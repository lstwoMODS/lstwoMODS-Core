using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;
public class Plot3DSurface : BaseUIElement<Plot3DSurface>
{
    public Plot3DSurface(string name, float[] xs, float[] ys, float[] zs, int rows, int cols, ImPlot3DSurfaceFlags flags = ImPlot3DSurfaceFlags.None) : base(name)
    {
        Data = new Plot3DSurfaceData { Name = name, XValues = xs ?? System.Array.Empty<float>(), YValues = ys ?? System.Array.Empty<float>(), ZValues = zs ?? System.Array.Empty<float>(), Rows = rows, Cols = cols, Flags = flags };
    }

    public void Update(float[] xs, float[] ys, float[] zs) { var d = (Plot3DSurfaceData)Data; d.XValues = xs; d.YValues = ys; d.ZValues = zs; MarkChanged(); }
    public Plot3DSurface WithFlags(ImPlot3DSurfaceFlags flags) { ((Plot3DSurfaceData)Data).Flags = flags; return this; }
}
