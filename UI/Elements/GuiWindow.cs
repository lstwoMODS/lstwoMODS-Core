using System;
using System.Collections.Generic;
using System.Linq;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class GuiWindow : BaseUIElement<GuiWindow>
{
    public List<BaseUIElement> Children;
    public Action<bool>? OnOpenChanged;
    public Action? OnFocused;
    private Ref<bool>? _openBinding;

    public bool Open => ((WindowData)Data).Open;

    public GuiWindow(string name, string windowTitle, params BaseUIElement[] children) : base(name)
    {
        Children = new List<BaseUIElement>(children);

        Data = new WindowData
        {
            Name        = name,
            WindowTitle = windowTitle,
            Open        = true,
            Children    = Children.Select(x => x.Data).ToList()
        };
    }

    /// <summary>
    /// Set the window size. Applied via SetNextWindowSize before the next Begin().
    /// <paramref name="cond"/> controls when it applies  Once (default) means only on first show,
    /// Always means every frame, FirstUseEver means only if no saved size exists.
    /// </summary>
    public GuiWindow WithSize(float width, float height, ImGuiCond cond = ImGuiCond.Once)
    {
        var d = (WindowData)Data; d.NextSizeX = width; d.NextSizeY = height; d.SizeCond = cond;
        return this;
    }

    /// <summary>
    /// Set the window position. Applied via SetNextWindowPos before the next Begin().
    /// <paramref name="pivotX"/>, <paramref name="pivotY"/>  0 = align top-left of window to pos,
    /// 0.5 = centre on pos, 1 = align bottom-right to pos.
    /// </summary>
    public GuiWindow WithPosition(float x, float y, ImGuiCond cond = ImGuiCond.Once,
                                   float pivotX = 0f, float pivotY = 0f)
    {
        var d = (WindowData)Data;
        d.NextPosX = x; d.NextPosY = y; d.PosCond = cond;
        d.PivotX   = pivotX; d.PivotY = pivotY;
        return this;
    }

    /// <summary>
    /// Convenience: center the window on screen using SetNextWindowPos with pivot (0.5, 0.5).
    /// Requires ImGuiConfigFlags.ViewportsEnable to be off, or the position to be in display-space.
    /// </summary>
    public GuiWindow Centered(ImGuiCond cond = ImGuiCond.Once)
    {
        // The renderer resolves negative sentinels to the display centre on each axis independently.
        var d = (WindowData)Data;
        d.NextPosX = -1f; d.NextPosY = -1f; d.PosCond = cond;
        d.PivotX   = 0.5f; d.PivotY = 0.5f;
        return this;
    }

    /// <summary>
    /// Pin the window horizontally centred at a fixed Y offset from the top of the display.
    /// Defaults to Always so the position is re-applied every frame.
    /// </summary>
    public GuiWindow CenteredX(float y, ImGuiCond cond = ImGuiCond.Always)
    {
        // Negative X sentinel → renderer resolves to displaySize.X / 2 at render time.
        var d = (WindowData)Data;
        d.NextPosX = -1f; d.NextPosY = y; d.PosCond = cond;
        d.PivotX   = 0.5f; d.PivotY = 0f;
        return this;
    }

    /// <summary>Bind the window title to a <see cref="Ref{T}"/>.</summary>
    public GuiWindow WithTitle(Ref<string> binding)
    {
        ((WindowData)Data).WindowTitle = binding.Value;
        binding.Changed += v => { ((WindowData)Data).WindowTitle = v; MarkChanged(); };
        return this;
    }

    /// <summary>Set ImGui window flags (e.g. NoResize, NoTitleBar, MenuBar). Chainable.</summary>
    public GuiWindow WithFlags(ImGuiWindowFlags flags)
    {
        ((WindowData)Data).WindowFlags = flags;
        return this;
    }

    /// <summary>Hide the close (X) button so the window cannot be dismissed by the user. Chainable.</summary>
    public GuiWindow WithNoClose()
    {
        ((WindowData)Data).ShowCloseButton = false;
        return this;
    }

    /// <summary>Bind the open/close state. Closing the window updates the ref.</summary>
    public GuiWindow WithOpen(Ref<bool> binding)
    {
        _openBinding = binding;
        ((WindowData)Data).Open = binding.Value;
        binding.Changed += v => { ((WindowData)Data).Open = v; MarkChanged(); };
        return this;
    }

    /// <summary>
    /// Set the inner scrollable content area size. Use to enable horizontal scrollbar.
    /// sizeY=0 means auto. Chainable.
    /// </summary>
    public GuiWindow WithContentSize(float sizeX, float sizeY = 0f)
    {
        var d = (WindowData)Data; d.ContentSizeX = sizeX; d.ContentSizeY = sizeY;
        return this;
    }

    /// <summary>
    /// Dock this window into a DockSpace on first use. Uses FirstUseEver by default so
    /// the user's layout changes are preserved after the first session.
    /// </summary>
    public GuiWindow WithDock(uint dockSpaceId, ImGuiCond cond = ImGuiCond.FirstUseEver)
    {
        var d = (WindowData)Data; d.DockId = dockSpaceId; d.DockCond = cond;
        return this;
    }

    /// <summary>
    /// Pin this window to the main viewport every frame. When enabled, the renderer calls
    /// SetNextWindowViewport(mainViewport.ID) before Begin() and treats positive X/Y on
    /// <see cref="WithPosition"/> as offsets from the main viewport's top-left rather than
    /// absolute display coordinates. Use for overlay-style elements that must stay on the
    /// primary display in multi-viewport setups but need explicit bottom/right anchoring.
    /// </summary>
    public GuiWindow PinToMainViewport(bool pin = true)
    {
        ((WindowData)Data).PinToMainViewport = pin;
        return this;
    }

    /// <summary>Set the initial open state. Chainable.</summary>
    public GuiWindow WithOpen(bool open)
    {
        ((WindowData)Data).Open = open;
        return this;
    }

    /// <summary>Bring this window to the front on its next render. Combine with opening the
    /// window (e.g. after setting <see cref="WithOpen(bool)"/> data) to switch tabs programmatically.</summary>
    public void FocusNextFrame()
    {
        var d = (WindowData)Data;
        d.FocusRequested = true;
        d.FocusRequestVersion++;
        MarkChanged();
    }

    /// <summary>
    /// Subscribe to open/close events. Fires whenever the window's open state changes,
    /// including the first frame the window is shown (initial render, menu reopen, F2 reopen).
    /// Set mainThread=false to fire the callback on the IPC thread instead of Unity's main thread.
    /// </summary>
    public GuiWindow OnOpen(Action<bool> callback, bool mainThread = true)
    {
        OnOpenChanged             = callback;
        RunCallbacksOnMainThread  = mainThread;
        return this;
    }

    /// <summary>
    /// Subscribe to focus-gained events. Fires when the window transitions from unfocused
    /// to focused (e.g. user clicks it). Suppressed on the first frame the window is shown 
    /// use <see cref="OnOpen"/> for that case.
    /// </summary>
    public GuiWindow OnFocus(Action callback, bool mainThread = true)
    {
        OnFocused                = callback;
        RunCallbacksOnMainThread = mainThread;
        return this;
    }

    public override IEnumerable<BaseUIElement> GetChildren() => Children;

    public override void ApplyReceivedData(BaseUIElementData data)
    {
        var prev        = (WindowData)Data;
        var wasOpen     = prev.Open;
        var wasFocused  = prev.Focused;
        var reqFocus    = prev.FocusRequested;
        var reqFocusVer = prev.FocusRequestVersion;
        base.ApplyReceivedData(data);
        var now         = (WindowData)Data;

        // The renderer never echoes these mod→overlay fields; keep our values so a focus
        // request isn't cleared (and its version isn't reset) by an incoming state sync.
        now.FocusRequested      = reqFocus;
        now.FocusRequestVersion = reqFocusVer;

        if (now.JustOpened)
        {
            if (_openBinding != null) _openBinding.Value = now.Open;
            var open = now.Open;
            InvokeCallback(() => OnOpenChanged?.Invoke(open));
            // Suppress focus event on the just-opened frame even though Focused likely
            // flipped false→true  OnOpen already represents this transition.
            return;
        }

        if (wasOpen != now.Open)
        {
            if (_openBinding != null) _openBinding.Value = now.Open;
            var open = now.Open;
            InvokeCallback(() => OnOpenChanged?.Invoke(open));
        }

        if (!wasFocused && now.Focused)
            InvokeCallback(() => OnFocused?.Invoke());
    }
}
