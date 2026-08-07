using System.Collections.Generic;
namespace lstwoMODS.ImGui.Shared.UI
{
    public class GraphNodeData : BaseUIElementData
    {
        public int    NodeId      { get; set; }
        public string NodeTitle   { get; set; } = "";
        public bool   HasTitleBar { get; set; } = true;
        // Optional initial position (null = ImNodes default)
        public float? InitX { get; set; } = null;
        public float? InitY { get; set; } = null;
        public List<BaseUIElementData> Children { get; set; } = new List<BaseUIElementData>();
    }
}
