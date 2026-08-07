using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;
public class PlotDragLine : BaseUIElement<PlotDragLine>
{
    public Action<double> OnValueChanged;
    public double Value { get => ((PlotDragLineData)Data).Value; set { ((PlotDragLineData)Data).Value = value; MarkChanged(); } }

    /// <param name="vertical">True = DragLineX (vertical line at x=value), False = DragLineY (horizontal at y=value)</param>
    public PlotDragLine(string name, int dragId, double value, bool vertical = true, Col? color = null, float thickness = 1f, ImPlotDragToolFlags flags = ImPlotDragToolFlags.None, Action<double> onValueChanged = null, bool mainThread = true) : base(name)
    {
        Col c = color ?? Color.red;
        Data = new PlotDragLineData { Name = name, DragId = dragId, Value = value, Vertical = vertical, ColorR = c.r, ColorG = c.g, ColorB = c.b, ColorA = c.a, Thickness = thickness, Flags = flags };
        OnValueChanged = onValueChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public override void ApplyReceivedData(BaseUIElementData data)
    {
        var old = Value;
        base.ApplyReceivedData(data);
        if (old != Value) { var v = Value; InvokeCallback(() => OnValueChanged?.Invoke(v)); }
    }
}
