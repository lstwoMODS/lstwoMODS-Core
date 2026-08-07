using System.Collections.Generic;

namespace lstwoMODS.ImGui.Shared.UI
{
    /// <summary>
    /// A transparent container: renders all children with no ImGui.BeginGroup/EndGroup wrapper.
    /// Disabling the container hides all children.
    /// </summary>
    public class ContainerData : BaseUIElementData
    {
        public List<BaseUIElementData> Children { get; set; } = new List<BaseUIElementData>();
    }
}
