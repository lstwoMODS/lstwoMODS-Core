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
        /// <summary>In <see cref="InsertBetween"/> mode, split the element top/bottom and draw a
        /// horizontal line instead of left/right and a vertical one. For a list that stacks down the
        /// screen, which is what a rail or a settings list is, the sideways version reads as
        /// nonsense: the line appears beside the row rather than in the gap it would land in.</summary>
        public bool     InsertVertical { get; set; } = false;

        /// <summary>In <see cref="InsertBetween"/> mode, whether the drop landed on the second half
        /// (insert after this element) rather than the first (insert before). That is the right half
        /// normally, and the bottom half under <see cref="InsertVertical"/>.</summary>
        public bool     DroppedAfter   { get; set; } = false;
        public List<BaseUIElementData> Children { get; set; } = new List<BaseUIElementData>();
    }
}
