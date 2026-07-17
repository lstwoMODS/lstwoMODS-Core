using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

/// <summary>ImGui.PlotHistogram  bar chart of float values.</summary>
public class PlotHistogram : BaseUIElement<PlotHistogram>
{
    public float[] Values
    {
        get => ((PlotHistogramData)Data).Values;
        set { ((PlotHistogramData)Data).Values = value; MarkChanged(); }
    }

    public PlotHistogram(string name, float[] values = null, string overlayText = null,
                         float scaleMin = float.MaxValue, float scaleMax = float.MaxValue,
                         float sizeX = 0f, float sizeY = 80f) : base(name)
    {
        Data = new PlotHistogramData
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
