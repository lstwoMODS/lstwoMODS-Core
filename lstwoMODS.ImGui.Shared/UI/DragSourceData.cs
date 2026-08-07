using System.Collections.Generic;

namespace lstwoMODS.ImGui.Shared.UI
{
    public class DragSourceData : BaseUIElementData
    {
        /// <summary>ImGui payload type string  used to match drag sources with compatible drop targets.</summary>
        public string PayloadType  { get; set; } = "";
        /// <summary>Arbitrary mod-defined payload data (any string; JSON, an ID, etc.).</summary>
        public string PayloadData  { get; set; } = "";
        /// <summary>Text shown in the drag tooltip. null = use PayloadType.</summary>
        public string DisplayLabel { get; set; } = null;
        public System.Collections.Generic.List<BaseUIElementData> Children { get; set; } = new System.Collections.Generic.List<BaseUIElementData>();
    }
}
