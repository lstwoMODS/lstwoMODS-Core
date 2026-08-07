using System.Collections.Generic;
namespace lstwoMODS.ImGui.Shared.UI
{
    public class NodeEditorData : BaseUIElementData
    {
        public List<BaseUIElementData> Children { get; set; } = new List<BaseUIElementData>();
        public List<NodeLinkData>      Links    { get; set; } = new List<NodeLinkData>();
        // Events reported by overlay
        public bool LinkCreated       { get; set; } = false;
        public int  NewLinkStartAttr  { get; set; } = -1;
        public int  NewLinkEndAttr    { get; set; } = -1;
        public bool LinkDestroyed     { get; set; } = false;
        public int  DestroyedLinkId   { get; set; } = -1;
        // Mini-map
        public bool ShowMiniMap { get; set; } = false;
        public ImNodesMiniMapLocation MiniMapLocation { get; set; } = ImNodesMiniMapLocation.BottomRight;
    }
}
