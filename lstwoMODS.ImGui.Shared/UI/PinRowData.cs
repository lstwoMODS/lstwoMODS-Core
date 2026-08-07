using System.Collections.Generic;

namespace lstwoMODS.ImGui.Shared.UI
{
    /// <summary>
    /// A single-line row: <see cref="Children"/> (lead) render left-to-right from the start of
    /// the row, while <see cref="LineChildren"/> (trail) are pinned to the right edge. When the
    /// trail won't fit beside the lead it wraps onto its own right-aligned line instead of
    /// overflowing  a header that stays on one line but degrades gracefully when narrow.
    ///
    /// Both lists deliberately use the framework's stripped collection names ("Children" /
    /// "LineChildren") so shallow update payloads never re-transmit the subtree and renderers
    /// preserve their existing children when an incoming update omits them.
    /// </summary>
    public class PinRowData : BaseUIElementData
    {
        /// <summary>Lead content, rendered in order from the left. Supply explicit SameLine
        /// elements between widgets that should share the line (same convention as Container).</summary>
        public List<BaseUIElementData> Children { get; set; } = new List<BaseUIElementData>();

        /// <summary>Trailing content pinned to the right edge (auto SameLine'd between items;
        /// wraps below, still right-aligned, when it won't fit beside the lead).</summary>
        public List<BaseUIElementData> LineChildren { get; set; } = new List<BaseUIElementData>();
    }
}
