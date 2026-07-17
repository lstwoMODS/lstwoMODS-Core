using System.Collections.Generic;

namespace lstwoMODS.ImGui.Shared.UI
{
    public class CollapsingHeaderData : BaseUIElementData
    {
        public string Label    { get; set; } = "";
        /// <summary>When true, renders an X close button. Set by WithClose().</summary>
        public bool   HasClose { get; set; } = false;
        /// <summary>Whether the header is still visible. Becomes false when the user clicks the X button.</summary>
        public bool   Visible  { get; set; } = true;
        /// <summary>Whether the header is currently expanded. Updated by the overlay when it changes.</summary>
        public bool   IsOpen   { get; set; } = false;
        public ImGuiTreeNodeFlags Flags { get; set; } = ImGuiTreeNodeFlags.None;
        public List<BaseUIElementData> Children { get; set; } = new List<BaseUIElementData>();

        // ── Drag & drop on the header bar (not the open body) ──────────────
        // Set by CollapsingHeader.WithDragSource / WithDropTarget.

        /// <summary>ImGui payload type making the header bar a drag source. Null = not draggable.</summary>
        public string DragPayloadType { get; set; }
        /// <summary>Arbitrary string payload delivered to the drop target.</summary>
        public string DragPayloadData { get; set; }
        /// <summary>Text shown in the drag tooltip. Null = use the header label.</summary>
        public string DragDisplayLabel { get; set; }
        /// <summary>Payload types the header bar accepts as a drop target. Null/empty = not a target.</summary>
        public string[] AcceptDropTypes { get; set; }
        /// <summary>Set by the overlay for one state report when a payload was dropped on the header bar.</summary>
        public string DroppedType { get; set; }
        public string DroppedPayload { get; set; }
        /// <summary>True when the drop landed on the lower half of the header bar (insert after); false = upper half (insert before).</summary>
        public bool DroppedBelow { get; set; }
    }
}
