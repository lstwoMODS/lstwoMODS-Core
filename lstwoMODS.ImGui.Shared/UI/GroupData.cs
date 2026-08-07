using System.Collections.Generic;

namespace lstwoMODS.ImGui.Shared.UI
{
    public class GroupData : BaseUIElementData
    {
        public List<BaseUIElementData> Children { get; set; } = new List<BaseUIElementData>();

        /// <summary>
        /// Child element ids in render order, or null to render in list order. Unlike
        /// <see cref="Children"/> (stripped from shallow UpdatedElements payloads), this
        /// survives the update path, so an existing group can be reordered at runtime 
        /// see Group.SetChildOrder.
        /// </summary>
        public int[] OrderedIds { get; set; }
    }
}
