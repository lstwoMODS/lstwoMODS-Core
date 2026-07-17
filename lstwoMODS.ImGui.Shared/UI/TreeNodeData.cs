using System.Collections.Generic;

namespace lstwoMODS.ImGui.Shared.UI
{
    public class TreeNodeData : BaseUIElementData
    {
        public string Label { get; set; } = "";
        public ImGuiTreeNodeFlags Flags { get; set; } = ImGuiTreeNodeFlags.None;
        public List<BaseUIElementData> Children { get; set; } = new List<BaseUIElementData>();

        /// <summary>Elements rendered on the node's line, after the label. Unlike
        /// <see cref="Children"/> they stay visible while the node is collapsed. Ordinary
        /// elements (buttons, text, ...)  their state syncs through the normal per-element
        /// protocol. Stripped from shallow updates like Children.</summary>
        public List<BaseUIElementData> LineChildren { get; set; } = new List<BaseUIElementData>();

        /// <summary>When true, <see cref="LineChildren"/> render pinned to the right edge of
        /// the node line instead of flowing after the label.</summary>
        public bool PinLineChildrenEnd { get; set; }

        /// <summary>Optional dimmed text between the label and the (pinned) line children 
        /// auto-ellipsized by the renderer to whatever width is available, so it can never
        /// overflow the line. Hidden entirely when there is almost no room.</summary>
        public string LineTag { get; set; }
        /// <summary>Hover tooltip on the line tag (e.g. the untruncated form).</summary>
        public string LineTagTooltip { get; set; }

        // ── Drag & drop on the node line (same semantics as CollapsingHeaderData) ──
        /// <summary>Non-null makes the node line an ImGui drag source of this payload type.</summary>
        public string DragPayloadType { get; set; }
        /// <summary>String payload delivered to the drop target.</summary>
        public string DragPayloadData { get; set; }
        /// <summary>Text shown in the drag tooltip. Null = use the label.</summary>
        public string DragDisplayLabel { get; set; }
        /// <summary>Non-empty makes the node line a drop target with insert-between semantics.</summary>
        public string[] AcceptDropTypes { get; set; }

        // One-shot overlay→mod drop report
        public string DroppedType { get; set; }
        public string DroppedPayload { get; set; }
        /// <summary>True when the drop landed on the lower half of the line (insert after).</summary>
        public bool DroppedBelow { get; set; }
    }
}
