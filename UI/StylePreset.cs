using System.Collections.Generic;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI;

/// <summary>
/// A reusable collection of ImGui push commands that can be applied to any element.
/// </summary>
public class StylePreset
{
    internal List<PushCommand> Commands { get; } = new List<PushCommand>();

    /// <summary>Push a font by name (registered via Window.AddFont). Pass null for default font.</summary>
    public StylePreset WithFont(string fontName)
    {
        Commands.Add(new PushFontCommand { FontName = fontName });
        return this;
    }

    /// <summary>Push a float-valued style var.</summary>
    public StylePreset WithStyleVar(ImGuiStyleVar var, float value)
    {
        Commands.Add(new PushStyleVarCommand { Var = var, Value = value });
        return this;
    }

    /// <summary>Push a Vec2-valued style var.</summary>
    public StylePreset WithStyleVar(ImGuiStyleVar var, float x, float y)
    {
        Commands.Add(new PushStyleVarVec2Command { Var = var, X = x, Y = y });
        return this;
    }

    /// <summary>Push a color override.</summary>
    public StylePreset WithStyleColor(ImGuiCol col, float r, float g, float b, float a)
    {
        Commands.Add(new PushStyleColorCommand { Col = col, R = r, G = g, B = b, A = a });
        return this;
    }

    /// <summary>
    /// Push a color override that keeps the configured RGB of <paramref name="col"/> and
    /// replaces only the alpha. RGB is resolved against the live style at render time.
    /// </summary>
    public StylePreset WithStyleColorAlpha(ImGuiCol col, float alpha)
    {
        Commands.Add(new PushStyleColorAlphaCommand { Col = col, A = alpha });
        return this;
    }

    /// <summary>Push a string ID.</summary>
    public StylePreset WithId(string id)
    {
        Commands.Add(new PushIdCommand { Id = id });
        return this;
    }

    /// <summary>Compose another preset into this one (copies its commands).</summary>
    public StylePreset WithPreset(StylePreset other)
    {
        Commands.AddRange(other.Commands);
        return this;
    }

    /// <summary>
    /// Set the width of child widget(s). Positive = pixels, 0 = default, -1 = fill remaining width.
    /// </summary>
    public StylePreset WithItemWidth(float width)
    {
        Commands.Add(new PushItemWidthCommand { Width = width });
        return this;
    }

    /// <summary>Apply ImGuiItemFlags to child widgets.</summary>
    public StylePreset WithItemFlags(ImGuiItemFlags flags, bool enable = true)
    {
        Commands.Add(new PushItemFlagCommand { Flags = flags, Enable = enable });
        return this;
    }

    /// <summary>Disable/enable child widgets (grayed out, non-interactive).</summary>
    public StylePreset WithDisabled(bool disabled = true)
    {
        Commands.Add(new PushDisabledCommand { Disabled = disabled });
        return this;
    }

    /// <summary>Set text wrap position. 0f = right edge of window, negative = no wrapping.</summary>
    public StylePreset WithTextWrapPos(float wrapPosX = 0f)
    {
        Commands.Add(new PushTextWrapPosCommand { WrapPosX = wrapPosX });
        return this;
    }

    /// <summary>Clip child rendering to a screen-space rectangle.</summary>
    public StylePreset WithClipRect(float minX, float minY, float maxX, float maxY, bool intersectWithCurrent = true)
    {
        Commands.Add(new PushClipRectCommand { MinX = minX, MinY = minY, MaxX = maxX, MaxY = maxY, IntersectWithCurrent = intersectWithCurrent });
        return this;
    }

    /// <summary>Control whether child widgets participate in tab-key navigation.</summary>
    public StylePreset WithTabStop(bool tabStop = true)
    {
        Commands.Add(new PushTabStopCommand { TabStop = tabStop });
        return this;
    }
}
