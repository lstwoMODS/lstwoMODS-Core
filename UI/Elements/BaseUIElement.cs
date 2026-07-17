using System;
using System.Collections.Generic;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public abstract class BaseUIElement
{
    public BaseUIElementData Data { get; set; }
    public string Name { get; set; }
    public BaseUIElement? Parent { get; internal set; }
    public bool WasDataChanged { get; set; }

    public bool IsTopLevel => Parent == null;

    /// <summary>
    /// When true (default), callbacks (OnValueChanged, OnPressed, etc.) are dispatched to the
    /// Unity main thread via MainThread.Queue, running during Plugin.Update().
    /// When false, callbacks fire immediately on the IPC reader thread, safe for non-Unity work
    /// and will still fire even when the game's main thread is frozen.
    /// </summary>
    public bool RunCallbacksOnMainThread { get; set; } = true;

    /// <summary>Routes a callback to the main thread or invokes it directly based on RunCallbacksOnMainThread.</summary>
    protected void InvokeCallback(Action cb)
    {
        if (RunCallbacksOnMainThread)
            MainThread.Enqueue(cb);
        else
            cb();
    }

    public void MarkChanged()
    {
        WasDataChanged = true;
    }

    public virtual void ApplyReceivedData(BaseUIElementData data)
    {
        // Preserve mod-side fields, overlay responses never include them
        data.PushCommands        = Data?.PushCommands ?? data.PushCommands;
        data.Tooltip             = Data?.Tooltip;
        data.TooltipHoveredFlags = Data?.TooltipHoveredFlags ?? ImGuiHoveredFlags.None;
        data.Enabled             = Data?.Enabled ?? data.Enabled;
        data.RequireInput        = Data?.RequireInput ?? data.RequireInput;
        Data = data;
    }

    public virtual IEnumerable<BaseUIElement> GetChildren() => [];

    public BaseUIElement(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Show or hide this element (and all its children) without removing it.
    /// Calls MarkChanged() automatically.
    /// </summary>
    public void SetVisible(bool visible)
    {
        Data.Enabled = visible;
        MarkChanged();
    }

    /// <summary>
    /// Toggle disabled state without removing the element from the UI.
    /// Removes any existing PushDisabledCommand then adds one if disabled=true.
    /// Calls MarkChanged() automatically.
    /// </summary>
    public void SetDisabled(bool disabled)
    {
        Data.PushCommands.RemoveAll(c => c is PushDisabledCommand);
        if (disabled)
            Data.PushCommands.Insert(0, new PushDisabledCommand { Disabled = true });
        MarkChanged();
    }

    /// <summary>True when this element type can hold children (exposes a Children list).</summary>
    public bool CanHoldChildren => GetElementChildrenList() != null && GetDataChildrenList() != null;

    /// <summary>
    /// Insert a child into this element's Children list and its Data mirror.
    /// Used by <see cref="OSWindow.AddElement(BaseUIElement, BaseUIElement, int)"/>
    /// call that instead of this so the overlay hears about the change.
    /// Returns false when this element type has no Children list.
    /// </summary>
    internal bool InsertChildAt(BaseUIElement child, int index)
    {
        var elements = GetElementChildrenList();
        var datas    = GetDataChildrenList();
        if (elements == null || datas == null) return false;

        if (index < 0 || index > elements.Count) index = elements.Count;
        elements.Insert(index, child);
        datas.Insert(Math.Min(index, datas.Count), child.Data);
        return true;
    }

    /// <summary>Counterpart of <see cref="InsertChildAt"/> for OSWindow.RemoveElement.</summary>
    internal bool RemoveChildElement(BaseUIElement child)
    {
        var elements = GetElementChildrenList();
        if (elements == null) return false;
        var removed = elements.Remove(child);
        GetDataChildrenList()?.Remove(child.Data);
        return removed;
    }

    // Every container-like element (Group, Container, ChildWindow, CollapsingHeader,
    // TreeNode, Modal, GuiWindow, ...) exposes "Children" as a List<BaseUIElement>
    // property or field, mirrored by a "Children" List<BaseUIElementData> on its Data.
    private List<BaseUIElement>? GetElementChildrenList()
    {
        var type = GetType();
        if (type.GetProperty("Children")?.GetValue(this) is List<BaseUIElement> viaProperty)
            return viaProperty;
        return type.GetField("Children")?.GetValue(this) as List<BaseUIElement>;
    }

    private List<BaseUIElementData>? GetDataChildrenList()
        => Data.GetType().GetProperty("Children")?.GetValue(Data) as List<BaseUIElementData>;
}

/// <summary>
/// Self-typed base that makes all fluent With* methods return the concrete element type,
/// so chains like <c>new DragFloat(...).WithItemWidth(-1f)</c> need no cast.
/// </summary>
public abstract class BaseUIElement<TSelf> : BaseUIElement where TSelf : BaseUIElement<TSelf>
{
    protected BaseUIElement(string name) : base(name) { }

    /// <summary>
    /// Show a tooltip when the user hovers over this element.
    /// <paramref name="hoveredFlags"/> controls timing/conditions:
    /// <list type="bullet">
    ///   <item><see cref="ImGuiHoveredFlags.None"/>: uses ImGui.SetItemTooltip (respects the style tooltip-delay setting, recommended default)</item>
    ///   <item><see cref="ImGuiHoveredFlags.ForTooltip"/>: same as None but explicit</item>
    ///   <item><see cref="ImGuiHoveredFlags.DelayShort"/> / <see cref="ImGuiHoveredFlags.DelayNormal"/>: explicit short/normal delay</item>
    ///   <item><see cref="ImGuiHoveredFlags.Stationary"/>: only show when mouse is still</item>
    /// </list>
    /// </summary>
    public TSelf WithTooltip(string text, ImGuiHoveredFlags hoveredFlags = ImGuiHoveredFlags.None)
    {
        Data.Tooltip             = text;
        Data.TooltipHoveredFlags = hoveredFlags;
        return (TSelf)this;
    }

    /// <summary>
    /// Bind the tooltip text to a <see cref="Ref{T}"/>. When the ref changes,
    /// the tooltip updates automatically without needing to keep an element reference.
    /// </summary>
    public TSelf WithTooltip(Ref<string> binding, ImGuiHoveredFlags hoveredFlags = ImGuiHoveredFlags.None)
    {
        Data.Tooltip             = binding.Value;
        Data.TooltipHoveredFlags = hoveredFlags;
        binding.Changed += v => { Data.Tooltip = v; MarkChanged(); };
        return (TSelf)this;
    }

    /// <summary>Push a font by name (registered via Window.AddFont). Pass null to push the default font.</summary>
    public TSelf WithFont(string fontName)
    {
        Data.PushCommands.Add(new PushFontCommand { FontName = fontName });
        return (TSelf)this;
    }

    /// <summary>Push a float-valued style var (e.g. FrameRounding, WindowRounding).</summary>
    public TSelf WithStyleVar(ImGuiStyleVar var, float value)
    {
        Data.PushCommands.Add(new PushStyleVarCommand { Var = var, Value = value });
        return (TSelf)this;
    }

    /// <summary>Push a Vec2-valued style var (e.g. WindowPadding, FramePadding).</summary>
    public TSelf WithStyleVar(ImGuiStyleVar var, float x, float y)
    {
        Data.PushCommands.Add(new PushStyleVarVec2Command { Var = var, X = x, Y = y });
        return (TSelf)this;
    }

    /// <summary>Push a color override for a style target.</summary>
    public TSelf WithStyleColor(ImGuiCol col, float r, float g, float b, float a)
    {
        Data.PushCommands.Add(new PushStyleColorCommand { Col = col, R = r, G = g, B = b, A = a });
        return (TSelf)this;
    }

    /// <summary>
    /// Push a color override that keeps the currently-configured RGB of <paramref name="col"/>
    /// and replaces only the alpha channel. The RGB is resolved against the live ImGui style
    /// at render time, so theme changes are tracked automatically.
    /// </summary>
    public TSelf WithStyleColorAlpha(ImGuiCol col, float alpha)
    {
        Data.PushCommands.Add(new PushStyleColorAlphaCommand { Col = col, A = alpha });
        return (TSelf)this;
    }

    /// <summary>Push a string onto the ImGui ID stack.</summary>
    public TSelf WithId(string id)
    {
        Data.PushCommands.Add(new PushIdCommand { Id = id });
        return (TSelf)this;
    }

    /// <summary>Apply a StylePreset: appends all of its push commands to this element.</summary>
    public TSelf WithPreset(StylePreset preset)
    {
        Data.PushCommands.AddRange(preset.Commands);
        return (TSelf)this;
    }

    /// <summary>
    /// Set the width of child widget(s). Positive = pixels, 0 = default, -1 = fill remaining width.
    /// </summary>
    public TSelf WithItemWidth(float width)
    {
        Data.PushCommands.Add(new PushItemWidthCommand { Width = width });
        return (TSelf)this;
    }

    /// <summary>
    /// Bind the item width to a <see cref="Ref{T}"/>. When the ref changes,
    /// the width updates automatically without needing to keep an element reference.
    /// </summary>
    public TSelf WithItemWidth(Ref<float> binding)
    {
        var cmd = new PushItemWidthCommand { Width = binding.Value };
        Data.PushCommands.Add(cmd);
        binding.Changed += v => { cmd.Width = v; MarkChanged(); };
        return (TSelf)this;
    }

    /// <summary>Apply ImGuiItemFlags to child widgets. Enable=true sets the flag, false clears it.</summary>
    public TSelf WithItemFlags(ImGuiItemFlags flags, bool enable = true)
    {
        Data.PushCommands.Add(new PushItemFlagCommand { Flags = flags, Enable = enable });
        return (TSelf)this;
    }

    /// <summary>Disable/enable child widgets (grayed out, non-interactive).</summary>
    public TSelf WithDisabled(bool disabled = true)
    {
        Data.PushCommands.Add(new PushDisabledCommand { Disabled = disabled });
        return (TSelf)this;
    }

    /// <summary>
    /// Bind the disabled state to a <see cref="Ref{T}"/>. When the ref changes,
    /// the element is enabled/disabled automatically without needing to keep an element reference.
    /// </summary>
    public TSelf WithDisabled(Ref<bool> binding)
    {
        // Apply initial state directly (no MarkChanged: element isn't registered yet)
        Data.PushCommands.RemoveAll(c => c is PushDisabledCommand);
        if (binding.Value)
            Data.PushCommands.Insert(0, new PushDisabledCommand { Disabled = true });
        binding.Changed += v => SetDisabled(v);
        return (TSelf)this;
    }

    /// <summary>
    /// Set text wrap position for child Text elements.
    /// 0f = wrap at right edge of window, negative = disable wrapping.
    /// </summary>
    public TSelf WithTextWrapPos(float wrapPosX = 0f)
    {
        Data.PushCommands.Add(new PushTextWrapPosCommand { WrapPosX = wrapPosX });
        return (TSelf)this;
    }

    /// <summary>Clip child rendering to a screen-space rectangle.</summary>
    public TSelf WithClipRect(float minX, float minY, float maxX, float maxY, bool intersectWithCurrent = true)
    {
        Data.PushCommands.Add(new PushClipRectCommand { MinX = minX, MinY = minY, MaxX = maxX, MaxY = maxY, IntersectWithCurrent = intersectWithCurrent });
        return (TSelf)this;
    }

    /// <summary>Control whether child widgets participate in tab-key navigation.</summary>
    public TSelf WithTabStop(bool binding)
    {
        Data.PushCommands.Add(new PushTabStopCommand { TabStop = binding });
        return (TSelf)this;
    }

    /// <summary>
    /// Bind the enabled/visible state to a <see cref="Ref{T}"/>. When the ref changes,
    /// the element shows or hides automatically without needing to keep an element reference.
    /// </summary>
    public TSelf WithVisible(Ref<bool> binding)
    {
        Data.Enabled = binding.Value;
        binding.Changed += v => { Data.Enabled = v; MarkChanged(); };
        return (TSelf)this;
    }

    /// <summary>
    /// Set whether this element (and its children) require mouse/keyboard focus.
    /// True/false override; use <see cref="RequireInputMode.Inherit"/> on the parent to let
    /// children decide individually.
    /// </summary>
    public TSelf WithRequireInput(bool requireInput)
    {
        Data.RequireInput = requireInput ? RequireInputMode.True : RequireInputMode.False;
        return (TSelf)this;
    }

    /// <summary>Explicitly set the tri-state <see cref="RequireInputMode"/> on this element.</summary>
    public TSelf WithRequireInput(RequireInputMode mode)
    {
        Data.RequireInput = mode;
        return (TSelf)this;
    }

    /// <summary>Bind RequireInput to a <see cref="Ref{T}"/>. Updates automatically when the ref changes.</summary>
    public TSelf WithRequireInput(Ref<bool> binding)
    {
        Data.RequireInput = binding.Value ? RequireInputMode.True : RequireInputMode.False;
        binding.Changed += v => { Data.RequireInput = v ? RequireInputMode.True : RequireInputMode.False; MarkChanged(); };
        return (TSelf)this;
    }

    /// <summary>
    /// Set whether callbacks fire on the Unity main thread (true, default) or immediately on the
    /// IPC reader thread (false). Chainable. Equivalent to setting RunCallbacksOnMainThread directly.
    /// </summary>
    public TSelf RunOnMainThread(Ref<bool> binding)
    {
        RunCallbacksOnMainThread = binding.Value;
        binding.Changed += v => { RunCallbacksOnMainThread = v; };
        return (TSelf)this;
    }
}
