using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;
public class PlotAnnotation : BaseUIElement<PlotAnnotation>
{
    public PlotAnnotation(string name, double x, double y, string text, bool clamp = false, float pixOffX = 0f, float pixOffY = -15f) : base(name)
    {
        Data = new PlotAnnotationData { Name = name, X = x, Y = y, Text = text, Clamp = clamp, PixOffX = pixOffX, PixOffY = pixOffY };
    }

    public PlotAnnotation WithColor(Color color) { var d = (PlotAnnotationData)Data; d.ColorR = color.r; d.ColorG = color.g; d.ColorB = color.b; d.ColorA = color.a; return this; }
    public PlotAnnotation WithColor(float r, float g, float b, float a = 1f) { var d = (PlotAnnotationData)Data; d.ColorR = r; d.ColorG = g; d.ColorB = b; d.ColorA = a; return this; }
    public PlotAnnotation WithOffset(float px, float py) { var d = (PlotAnnotationData)Data; d.PixOffX = px; d.PixOffY = py; return this; }
}
