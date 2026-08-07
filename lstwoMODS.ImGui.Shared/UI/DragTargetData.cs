using System.Collections.Generic;

namespace lstwoMODS.ImGui.Shared.UI
{
    public class DragTargetData : BaseUIElementData
    {
        /// <summary>Payload type strings this target accepts. Multiple types = accepts any of them.</summary>
        public string[] AcceptTypes    { get; set; } = System.Array.Empty<string>();
        /// <summary>Set by the overlay when a drop completes. null = no drop this frame.</summary>
        public string   DroppedType    { get; set; } = null;
        /// <summary>The payload data from the dropped source. null = no drop this frame.</summary>
        public string   DroppedPayload { get; set; } = null;
        /// <summary>Insert-between mode: instead of highlighting the whole element as a drop box,
        /// draw a vertical insertion line at the left or right edge (whichever half the cursor is
        /// over) and report the side via <see cref="DroppedAfter"/>. For reordering a horizontal
        /// row/grid of items by dropping into the gaps between them.</summary>
        public bool     InsertBetween  { get; set; } = false;
        /// <summary>In <see cref="InsertBetween"/> mode, whether the drop landed on the right half
        /// (insert after this element) rather than the left (insert before).</summary>
        public bool     DroppedAfter   { get; set; } = false;
        public List<BaseUIElementData> Children { get; set; } = new List<BaseUIElementData>();
    }
}
