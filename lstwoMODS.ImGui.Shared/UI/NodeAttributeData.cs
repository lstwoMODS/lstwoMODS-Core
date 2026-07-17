using System.Collections.Generic;
namespace lstwoMODS.ImGui.Shared.UI
{
    public abstract class NodeAttributeData : BaseUIElementData
    {
        public int AttributeId { get; set; }
        public ImNodesPinShape PinShape { get; set; } = ImNodesPinShape.CircleFilled;
        public List<BaseUIElementData> Children { get; set; } = new List<BaseUIElementData>();
    }

    public class InputAttributeData : NodeAttributeData { }
    public class OutputAttributeData : NodeAttributeData { }
    public class StaticAttributeData : NodeAttributeData { }
}
