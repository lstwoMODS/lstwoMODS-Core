using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

/// <summary>ImGui.Separator()  draws a horizontal line.</summary>
public class Separator : BaseUIElement<Separator>
{
    public Separator(string name) : base(name)
    {
        Data = new SeparatorData { Name = name };
    }
}

/// <summary>ImGui.Spacing()  adds ItemSpacing.y vertical space.</summary>
public class Spacing : BaseUIElement<Spacing>
{
    public Spacing(string name) : base(name)
    {
        Data = new SpacingData { Name = name };
    }
}

/// <summary>ImGui.NewLine()  moves the cursor to the next line.</summary>
public class NewLine : BaseUIElement<NewLine>
{
    public NewLine(string name) : base(name)
    {
        Data = new NewLineData { Name = name };
    }
}

/// <summary>ImGui.SameLine()  places the next widget on the same line.</summary>
public class SameLine : BaseUIElement<SameLine>
{
    /// <param name="offsetX">Absolute X position from window left. 0 = use cursor + spacing.</param>
    /// <param name="spacing">Spacing override. -1 = use ItemSpacing.x.</param>
    public SameLine(string name, float offsetX = 0f, float spacing = -1f) : base(name)
    {
        Data = new SameLineData { Name = name, OffsetX = offsetX, Spacing = spacing };
    }
}

/// <summary>ImGui.Dummy()  invisible placeholder item with a given size.</summary>
public class Dummy : BaseUIElement<Dummy>
{
    public Dummy(string name, float sizeX, float sizeY) : base(name)
    {
        Data = new DummyData { Name = name, SizeX = sizeX, SizeY = sizeY };
    }
}

/// <summary>ImGui.Indent() / ImGui.Unindent()  shifts content horizontally.</summary>
public class Indent : BaseUIElement<Indent>
{
    /// <param name="amount">Pixel amount. 0 = use IndentSpacing style value.</param>
    /// <param name="unindent">True to unindent instead of indent.</param>
    public Indent(string name, float amount = 0f, bool unindent = false) : base(name)
    {
        Data = new IndentData { Name = name, Amount = amount, Unindent = unindent };
    }
}

/// <summary>ImGui.AlignTextToFramePadding()  call before a Text() to vertically centre it alongside taller widgets like buttons or inputs.</summary>
public class AlignText : BaseUIElement<AlignText>
{
    public AlignText(string name) : base(name) { Data = new AlignTextData { Name = name }; }
}

/// <summary>ImGui.SetCursorPos(x, y) or ImGui.SetCursorScreenPos(x, y) for manual element positioning.</summary>
public class SetCursorPos : BaseUIElement<SetCursorPos>
{
    public SetCursorPos(string name, float x, float y, bool screenSpace = false) : base(name)
    {
        Data = new SetCursorPosData { Name = name, X = x, Y = y, ScreenSpace = screenSpace };
    }
}

/// <summary>ImGui.SetNextItemWidth  sets the width of the very next widget only (no matching pop needed).</summary>
public class SetNextItemWidth : BaseUIElement<SetNextItemWidth>
{
    public SetNextItemWidth(string name, float width) : base(name)
    {
        Data = new SetNextItemWidthData { Name = name, Width = width };
    }
}

/// <summary>ImGui.Columns(count)  legacy multi-column layout. Use NextColumn to advance between columns. Reset with Columns(1).</summary>
public class Columns : BaseUIElement<Columns>
{
    public Columns(string name, int count = 1, bool borders = true, string id = null) : base(name)
    {
        Data = new ColumnsData { Name = name, Count = count, Borders = borders, ColId = id };
    }
}

/// <summary>ImGui.NextColumn()  advance to the next legacy column.</summary>
public class NextColumn : BaseUIElement<NextColumn>
{
    public NextColumn(string name) : base(name) { Data = new NextColumnData { Name = name }; }
}

/// <summary>ImGui.SetKeyboardFocusHere(offset)  give keyboard focus to the next (0) or nth widget, or previous (-1).</summary>
public class FocusNext : BaseUIElement<FocusNext>
{
    public FocusNext(string name, int offset = 0) : base(name)
    {
        Data = new FocusNextData { Name = name, Offset = offset };
    }
}

/// <summary>ImGui.SetItemDefaultFocus()  mark the previously rendered item as the default keyboard focus target (e.g. in combo dropdowns).</summary>
public class FocusDefault : BaseUIElement<FocusDefault>
{
    public FocusDefault(string name) : base(name) { Data = new FocusDefaultData { Name = name }; }
}

/// <summary>
/// ImGui.SetNextItemShortcut(keyChord, flags)  assigns a keyboard shortcut to the next widget.
/// Build keyChord: (int)ImGuiKey.S | (int)ImGuiKey.ModCtrl for Ctrl+S.
/// </summary>
public class NextItemShortcut : BaseUIElement<NextItemShortcut>
{
    /// <param name="keyChord">(int)ImGuiKey.Key | (int)ImGuiKey.ModCtrl etc.</param>
    public NextItemShortcut(string name, int keyChord, ImGuiInputFlags flags = ImGuiInputFlags.None) : base(name)
    {
        Data = new SetNextItemShortcutData { Name = name, KeyChord = keyChord, InputFlags = flags };
    }
    /// <summary>Convenience overload: pass key and optional modifiers separately.</summary>
    public NextItemShortcut(string name, ImGuiKey key, ImGuiKey mod1 = 0, ImGuiKey mod2 = 0, ImGuiInputFlags flags = ImGuiInputFlags.None)
        : this(name, (int)key | (int)mod1 | (int)mod2, flags) { }
}
