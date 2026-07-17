namespace lstwoMODS.ImGui.Shared.UI
{
    public class SeparatorData     : BaseUIElementData { }

    public class SeparatorTextData : BaseUIElementData
    {
        public string Label { get; set; } = "";
    }

    public class SpacingData : BaseUIElementData { }

    public class NewLineData : BaseUIElementData { }

    public class SameLineData : BaseUIElementData
    {
        public float OffsetX { get; set; } = 0f;   // 0 = auto
        public float Spacing { get; set; } = -1f;  // -1 = use default ItemSpacing.x
    }

    public class DummyData : BaseUIElementData
    {
        public float SizeX { get; set; } = 0f;
        public float SizeY { get; set; } = 0f;
    }

    public class IndentData : BaseUIElementData
    {
        public float Amount   { get; set; } = 0f;     // 0 = use IndentSpacing style
        public bool  Unindent { get; set; } = false;
    }

    /// <summary>ImGui.AlignTextToFramePadding()  aligns text baseline to centre of frame (for mixing text with buttons/inputs on the same line).</summary>
    public class AlignTextData : BaseUIElementData { }

    /// <summary>ImGui.SetCursorPos() or ImGui.SetCursorScreenPos() for manual cursor positioning.</summary>
    public class SetCursorPosData : BaseUIElementData
    {
        public float X           { get; set; } = 0f;
        public float Y           { get; set; } = 0f;
        public bool  ScreenSpace { get; set; } = false;  // true = SetCursorScreenPos (absolute), false = SetCursorPos (window-relative)
    }

    /// <summary>ImGui.SetNextItemWidth()  sets the width of the very next widget only (no pop needed).</summary>
    public class SetNextItemWidthData : BaseUIElementData
    {
        public float Width { get; set; } = 0f;  // positive = pixels, -1 = fill remaining
    }

    /// <summary>ImGui.Columns(count, id, border)  legacy multi-column layout.</summary>
    public class ColumnsData : BaseUIElementData
    {
        public int    Count   { get; set; } = 1;
        public string ColId   { get; set; } = null;  // null = no id
        public bool   Borders { get; set; } = true;
    }

    /// <summary>ImGui.NextColumn()  advance to the next legacy column.</summary>
    public class NextColumnData : BaseUIElementData { }

    /// <summary>ImGui.SetKeyboardFocusHere(offset)  focus the next (offset=0) or nth widget.</summary>
    public class FocusNextData : BaseUIElementData
    {
        public int Offset { get; set; } = 0;  // 0 = next widget, -1 = previous
    }

    /// <summary>ImGui.SetItemDefaultFocus()  mark the current item as the default focused item.</summary>
    public class FocusDefaultData : BaseUIElementData { }

    /// <summary>ImGui.SetNextItemShortcut(keyChord, flags)  keyboard shortcut for the next widget.</summary>
    public class SetNextItemShortcutData : BaseUIElementData
    {
        /// <summary>Key chord: (int)ImGuiKey.S | (int)ImGuiKey.ModCtrl for Ctrl+S.</summary>
        public int KeyChord { get; set; } = 0;
        public ImGuiInputFlags InputFlags { get; set; } = ImGuiInputFlags.None;
    }
}
