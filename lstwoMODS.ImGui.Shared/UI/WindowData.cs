using System.Collections.Generic;

namespace lstwoMODS.ImGui.Shared.UI
{
    public class WindowData : BaseUIElementData
    {
        public string            WindowTitle { get; set; } = "";
        public List<BaseUIElementData> Children { get; set; } = new List<BaseUIElementData>();
        public bool              Open            { get; set; } = true;
        public bool              ShowCloseButton { get; set; } = true;
        public ImGuiWindowFlags  WindowFlags     { get; set; } = ImGuiWindowFlags.None;

        // Overlay→mod only. Set to true on the single frame the window transitions
        // from not-rendered to rendered (initial show, menu reopen, F2 reopen).
        public bool              JustOpened      { get; set; } = false;

        // Overlay→mod only. Reflects ImGui.IsWindowFocused(RootAndChildWindows).
        public bool              Focused         { get; set; } = false;

        // Mod→overlay. Set true and bump FocusRequestVersion to bring this window to the front
        // on its next render (e.g. programmatically switching to a tab). The renderer triggers
        // once per new version, so repeat requests re-focus. Never sent back overlay→mod.
        public bool              FocusRequested       { get; set; } = false;
        public int               FocusRequestVersion  { get; set; } = 0;

        public float?      NextSizeX    { get; set; } = null;  // null = don't call SetNextWindowSize
        public float?      NextSizeY    { get; set; } = null;
        public ImGuiCond   SizeCond     { get; set; } = ImGuiCond.Once;

        public float?      NextPosX     { get; set; } = null;  // null = don't call SetNextWindowPos
        public float?      NextPosY     { get; set; } = null;
        public ImGuiCond   PosCond      { get; set; } = ImGuiCond.Once;
        // Pivot for position (0,0 = top-left, 0.5,0.5 = centre, 1,1 = bottom-right)
        public float       PivotX       { get; set; } = 0f;
        public float       PivotY       { get; set; } = 0f;

        public float? ContentSizeX { get; set; } = null;  // null = don't call
        public float? ContentSizeY { get; set; } = null;

        public uint?       DockId   { get; set; } = null;  // null = don't call
        public ImGuiCond   DockCond { get; set; } = ImGuiCond.FirstUseEver;

        /// <summary>
        /// Pin this window to the main viewport every frame (calls SetNextWindowViewport(mv.ID)
        /// before Begin), and treat NextPosX/Y as offsets relative to the main viewport's
        /// top-left rather than absolute display coordinates. Use when you need positive
        /// bottom-left/right positioning that the negative-sentinel system can't express.
        /// </summary>
        public bool        PinToMainViewport { get; set; } = false;
    }
}
