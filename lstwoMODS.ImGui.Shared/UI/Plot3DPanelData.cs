using System.Collections.Generic;
namespace lstwoMODS.ImGui.Shared.UI
{
    public class Plot3DPanelData : BaseUIElementData
    {
        public string Title  { get; set; } = "3D Plot";
        public float  SizeX  { get; set; } = -1f;
        public float  SizeY  { get; set; } = 300f;
        public ImPlot3DFlags Flags { get; set; } = ImPlot3DFlags.None;
        public string XLabel { get; set; } = null;
        public string YLabel { get; set; } = null;
        public string ZLabel { get; set; } = null;
        public ImPlot3DAxisFlags XFlags { get; set; } = ImPlot3DAxisFlags.None;
        public ImPlot3DAxisFlags YFlags { get; set; } = ImPlot3DAxisFlags.None;
        public ImPlot3DAxisFlags ZFlags { get; set; } = ImPlot3DAxisFlags.None;
        public List<BaseUIElementData> Children { get; set; } = new List<BaseUIElementData>();
    }
}
