namespace lstwoMODS.ImGui.Shared.UI
{
    public abstract class PushCommand { }

    /// <summary>Push a named font. FontName null = push default font (ImGui.PushFont(null)).</summary>
    public class PushFontCommand : PushCommand
    {
        public string FontName { get; set; }
    }

    /// <summary>Push a float-valued style var.</summary>
    public class PushStyleVarCommand : PushCommand
    {
        public ImGuiStyleVar Var   { get; set; }
        public float         Value { get; set; }
    }

    /// <summary>Push a Vec2-valued style var (e.g. WindowPadding, FramePadding).</summary>
    public class PushStyleVarVec2Command : PushCommand
    {
        public ImGuiStyleVar Var { get; set; }
        public float         X   { get; set; }
        public float         Y   { get; set; }
    }

    /// <summary>Push a style color override.</summary>
    public class PushStyleColorCommand : PushCommand
    {
        public ImGuiCol Col { get; set; }
        public float    R   { get; set; }
        public float    G   { get; set; }
        public float    B   { get; set; }
        public float    A   { get; set; }
    }

    /// <summary>
    /// Push a style color override that keeps the currently-configured RGB of
    /// <see cref="Col"/> and only replaces the alpha channel. The RGB is read from the
    /// live ImGui style at render time, so changes to the theme are tracked automatically.
    /// </summary>
    public class PushStyleColorAlphaCommand : PushCommand
    {
        public ImGuiCol Col { get; set; }
        public float    A   { get; set; }
    }

    /// <summary>Push an ID onto the ImGui ID stack.</summary>
    public class PushIdCommand : PushCommand
    {
        public string Id { get; set; }
    }

    /// <summary>
    /// Set the width of the next widget(s). Positive = pixels. 0 = default. -1 = fill remaining width.
    /// </summary>
    public class PushItemWidthCommand : PushCommand
    {
        public float Width { get; set; }
    }

    /// <summary>
    /// Apply ImGuiItemFlags to child widgets. Enable=true sets the flag, Enable=false clears it.
    /// </summary>
    public class PushItemFlagCommand : PushCommand
    {
        public ImGuiItemFlags Flags  { get; set; }
        public bool           Enable { get; set; } = true;
    }

    /// <summary>
    /// Disable child widgets (grayed out, non-interactive). Maps to ImGui.BeginDisabled/EndDisabled.
    /// </summary>
    public class PushDisabledCommand : PushCommand
    {
        public bool Disabled { get; set; } = true;
    }

    /// <summary>
    /// Set the text wrap position for subsequent Text calls.
    /// 0f = wrap at right edge of window. Negative = disable wrapping.
    /// </summary>
    public class PushTextWrapPosCommand : PushCommand
    {
        public float WrapPosX { get; set; } = 0f;
    }

    /// <summary>
    /// Clip rendering to a screen-space rectangle.
    /// </summary>
    public class PushClipRectCommand : PushCommand
    {
        public float MinX                   { get; set; }
        public float MinY                   { get; set; }
        public float MaxX                   { get; set; }
        public float MaxY                   { get; set; }
        public bool  IntersectWithCurrent   { get; set; } = true;
    }

    /// <summary>
    /// Control whether the next widget participates in tab-key navigation.
    /// </summary>
    public class PushTabStopCommand : PushCommand
    {
        public bool TabStop { get; set; } = true;
    }
}
