using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;
public class Plot3DPanel : BaseUIElement<Plot3DPanel>
{
    public List<BaseUIElement> Children;

    public Plot3DPanel(string name, string title, float sizeX = -1f, float sizeY = 300f, params BaseUIElement[] children) : base(name)
    {
        Children = new List<BaseUIElement>(children);
        Data = new Plot3DPanelData { Name = name, Title = title, SizeX = sizeX, SizeY = sizeY, Children = Children.Select(c => c.Data).ToList() };
    }

    public Plot3DPanel WithFlags(ImPlot3DFlags flags) { ((Plot3DPanelData)Data).Flags = flags; return this; }
    public Plot3DPanel WithAxes(string x = null, string y = null, string z = null,
        ImPlot3DAxisFlags xf = ImPlot3DAxisFlags.None, ImPlot3DAxisFlags yf = ImPlot3DAxisFlags.None, ImPlot3DAxisFlags zf = ImPlot3DAxisFlags.None)
    { var d = (Plot3DPanelData)Data; d.XLabel = x; d.YLabel = y; d.ZLabel = z; d.XFlags = xf; d.YFlags = yf; d.ZFlags = zf; return this; }

    public override IEnumerable<BaseUIElement> GetChildren() => Children;
}
