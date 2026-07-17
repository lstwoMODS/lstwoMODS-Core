using System.Collections.Generic;

namespace lstwoMODS.ImGui.Shared.UI
{
    public class ChildWindowData : BaseUIElementData
    {
        public float SizeX { get; set; } = 0f;
        public float SizeY { get; set; } = 0f;

        // When > 0, overrides SizeY with -(lines * GetFrameHeightWithSpacing()): the child
        // fills the remaining height minus room for that many footer widget rows below it
        // (computed overlay-side  only the renderer knows the font/frame metrics).
        public float ReserveFooterLines { get; set; } = 0f;
        public ImGuiChildFlags  ChildFlags  { get; set; } = ImGuiChildFlags.None;
        public ImGuiWindowFlags WindowFlags { get; set; } = ImGuiWindowFlags.None;
        public List<BaseUIElementData> Children { get; set; } = new List<BaseUIElementData>();

        // Scroll control  set to a value to scroll on the next render frame, then auto-cleared by the renderer
        public float? ScrollHereY { get; set; } = null;  // 0=top, 0.5=center, 1.0=bottom
        public float? ScrollHereX { get; set; } = null;
        public float? ScrollToY   { get; set; } = null;  // absolute pixel offset
        public float? ScrollToX   { get; set; } = null;
    }
}
