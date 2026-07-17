using System;
using System.Collections.Generic;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS.ImGui.Shared
{
    // Verified against Hexa.NET.ImGui 2.2.9 via reflection.
    [Flags]
    public enum ImGuiConfigFlags
    {
        None              = 0,
        NavEnableKeyboard = 1,
        NavEnableGamepad  = 2,
        NoMouse           = 16,
        NoMouseCursorChange = 32,
        NoKeyboard        = 64,
        DockingEnable     = 128,
        ViewportsEnable   = 1024,
        IsSrgb            = 1048576,
        IsTouchScreen     = 2097152,
    }

    public class ImGuiConfig
    {
        // ── Config flags ─────────────────────────────────────────────────────
        public ImGuiConfigFlags ConfigFlags { get; set; } =
            ImGuiConfigFlags.NavEnableKeyboard |
            ImGuiConfigFlags.NavEnableGamepad  |
            ImGuiConfigFlags.DockingEnable     |
            ImGuiConfigFlags.ViewportsEnable;

        // ── Mouse / cursor ───────────────────────────────────────────────────
        public bool  MouseDrawCursor          { get; set; } = false;
        public bool  MouseCtrlLeftAsRightClick{ get; set; } = false;
        public float MouseDoubleClickTime     { get; set; } = 0.3f;
        public float MouseDoubleClickMaxDist  { get; set; } = 6.0f;
        public float MouseDragThreshold       { get; set; } = 6.0f;

        // ── Keyboard / input ─────────────────────────────────────────────────
        public float KeyRepeatDelay              { get; set; } = 0.275f;
        public float KeyRepeatRate               { get; set; } = 0.05f;
        public bool  ConfigInputTextCursorBlink  { get; set; } = true;
        public bool  ConfigInputTextEnterKeepActive { get; set; } = false;
        public bool  ConfigDragClickToInputText  { get; set; } = false;
        public bool  ConfigInputTrickleEventQueue { get; set; } = true;
        public bool  ConfigMacOSXBehaviors       { get; set; } = false;

        // ── Navigation ───────────────────────────────────────────────────────
        public bool ConfigNavSwapGamepadButtons    { get; set; } = false;
        public bool ConfigNavCaptureKeyboard       { get; set; } = true;
        public bool ConfigNavCursorVisibleAlways   { get; set; } = false;
        public bool ConfigNavCursorVisibleAuto     { get; set; } = true;
        public bool ConfigNavEscapeClearFocusItem  { get; set; } = true;
        public bool ConfigNavEscapeClearFocusWindow{ get; set; } = false;
        public bool ConfigNavMoveSetMousePos       { get; set; } = false;

        // ── Windows ──────────────────────────────────────────────────────────
        public bool ConfigWindowsResizeFromEdges     { get; set; } = true;
        public bool ConfigWindowsMoveFromTitleBarOnly{ get; set; } = false;
        public bool ConfigWindowsCopyContentsWithCtrlC { get; set; } = false;
        public bool ConfigScrollbarScrollByPage      { get; set; } = true;

        // ── Docking ──────────────────────────────────────────────────────────
        /// <summary>
        /// ID passed to DockSpaceOverViewport each frame. 0 (default) lets ImGui auto-generate one
        /// from the viewport. Set to a stable non-zero value to give the global dock space a
        /// known ID, so you can dock windows into it via <c>GuiWindow.WithDock(id)</c>.
        /// </summary>
        public uint DockSpaceOverViewportId         { get; set; } = 0;
        public bool ConfigDockingAlwaysTabBar        { get; set; } = false;
        public bool ConfigDockingNoSplit             { get; set; } = false;
        public bool ConfigDockingTransparentPayload  { get; set; } = false;
        public bool ConfigDockingWithShift           { get; set; } = false;

        // ── Viewports ────────────────────────────────────────────────────────
        public bool ConfigViewportsNoAutoMerge              { get; set; } = false;
        public bool ConfigViewportsNoDecoration             { get; set; } = true;
        public bool ConfigViewportsNoDefaultParent          { get; set; } = false;
        public bool ConfigViewportsNoTaskBarIcon            { get; set; } = false;
        public bool ConfigViewportPlatformFocusSetsImGuiFocus { get; set; } = true;

        // ── Fonts / DPI ──────────────────────────────────────────────────────
        public float FontGlobalScale       { get; set; } = 1.0f;
        public bool  FontAllowUserScaling  { get; set; } = false;
        public bool  ConfigDpiScaleFonts   { get; set; } = true;
        public bool  ConfigDpiScaleViewports { get; set; } = true;

        // ── Memory / ini ─────────────────────────────────────────────────────
        public float ConfigMemoryCompactTimer { get; set; } = 60.0f;
        public float IniSavingRate            { get; set; } = 5.0f;
        /// <summary>Set to true to disable ImGui writing layout state to imgui.ini.</summary>
        public bool  DisableIniSave           { get; set; } = false;

        // ── Error recovery ───────────────────────────────────────────────────
        public bool ConfigErrorRecovery              { get; set; } = true;
        public bool ConfigErrorRecoveryEnableAssert  { get; set; } = true;
        public bool ConfigErrorRecoveryEnableDebugLog{ get; set; } = true;
        public bool ConfigErrorRecoveryEnableTooltip { get; set; } = true;

        // ── Debug ────────────────────────────────────────────────────────────
        public bool ConfigDebugIsDebuggerPresent            { get; set; } = false;
        public bool ConfigDebugBeginReturnValueOnce         { get; set; } = false;
        public bool ConfigDebugBeginReturnValueLoop         { get; set; } = false;
        public bool ConfigDebugIgnoreFocusLoss              { get; set; } = false;
        public bool ConfigDebugIniSettings                  { get; set; } = false;
        public bool ConfigDebugHighlightIdConflicts         { get; set; } = true;
        public bool ConfigDebugHighlightIdConflictsShowItemPicker { get; set; } = true;

        // ── Base theme + global style overrides ─────────────────────────────
        /// <summary>
        /// Base color theme applied before <see cref="GlobalStyle"/> overrides.
        /// 0 = none (keep ImGui default), 1 = Dark, 2 = Light, 3 = Classic.
        /// </summary>
        public int BaseTheme { get; set; } = 0;

        /// <summary>
        /// Applied permanently to ImGuiStyle (not push/pop).
        /// Supports PushStyleVarCommand, PushStyleVarVec2Command, PushStyleColorCommand.
        /// Applied after the base theme.
        /// </summary>
        public List<PushCommand> GlobalStyle { get; set; } = new List<PushCommand>();
    }
}
