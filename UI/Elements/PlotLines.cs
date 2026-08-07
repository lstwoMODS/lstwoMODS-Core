using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

/// <summary>ImGui.PlotLines: line graph of float values. Update Values and call MarkChanged() each frame.</summary>
public class PlotLines : BaseUIElement<PlotLines>
{
    public float[] Values
    {
        get => ((PlotLinesData)Data).Values;
        set { ((PlotLinesData)Data).Values = value; MarkChanged(); }
    }

    public PlotLines(string name, float[] values = null, string overlayText = null,
                     float scaleMin = float.MaxValue, float scaleMax = float.MaxValue,
                     float sizeX = 0f, float sizeY = 80f) : base(name)
    {
        Data = new PlotLinesData
        {
            Name        = name,
            Values      = values ?? System.Array.Empty<float>(),
            OverlayText = overlayText,
            ScaleMin    = scaleMin,
            ScaleMax    = scaleMax,
            SizeX       = sizeX,
            SizeY       = sizeY
        };
    }
}
