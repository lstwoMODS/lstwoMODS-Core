using System;
using System.Collections.Generic;
using System.Linq;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class CollapsingHeader : BaseUIElement<CollapsingHeader>
{
    public List<BaseUIElement> Children;
    public Action<bool>? OnToggled;
    /// <summary>
    /// Fired when a compatible payload is dropped on the header bar.
    /// Parameters: (payloadType, payloadData, droppedBelow), droppedBelow is true when the
    /// drop landed on the lower half of the bar (insert after), false for the upper half
    /// (insert before). See <see cref="WithDropTarget"/>.
    /// </summary>
    public Action<string, string, bool>? OnDrop;

    /// <param name="name">Unique element ID (used by IPC registry).</param>
    /// <param name="label">Text shown in the header bar.</param>
    public CollapsingHeader(string name, string label, params BaseUIElement[] children) : base(name)
    {
        Children = new List<BaseUIElement>(children);
        Data = new CollapsingHeaderData
        {
            Name     = name,
            Label    = label,
            Children = Children.Select(c => c.Data).ToList()
        };
    }

    public string Label
    {
        get => ((CollapsingHeaderData)Data).Label;
        set { ((CollapsingHeaderData)Data).Label = value; MarkChanged(); }
    }

    /// <summary>Bind the header label to a <see cref="Ref{T}"/>.</summary>
    public CollapsingHeader WithLabel(Ref<string> binding)
    {
        ((CollapsingHeaderData)Data).Label = binding.Value;
        binding.Changed += v => { ((CollapsingHeaderData)Data).Label = v; MarkChanged(); };
        return this;
    }

    /// <summary>Start expanded. Chainable.</summary>
    public CollapsingHeader DefaultOpen() { ((CollapsingHeaderData)Data).Flags |= ImGuiTreeNodeFlags.DefaultOpen; return this; }

    /// <summary>Set tree node flags. Chainable.</summary>
    public CollapsingHeader WithFlags(ImGuiTreeNodeFlags flags) { ((CollapsingHeaderData)Data).Flags = flags; return this; }

    /// <summary>Show an X button; fires OnToggled(false) when the user clicks it. Chainable.</summary>
    public CollapsingHeader WithClose() { ((CollapsingHeaderData)Data).HasClose = true; return this; }

    /// <summary>Subscribe to open/close events. Chainable.</summary>
    public CollapsingHeader OnToggle(Action<bool> cb, bool mainThread = true)
    {
        OnToggled = cb; RunCallbacksOnMainThread = mainThread; return this;
    }

    /// <summary>
    /// Make the header bar an ImGui drag source (the open body stays interactive as normal).
    /// Click-to-expand keeps working  a drag only starts past the mouse drag threshold.
    /// Chainable.
    /// </summary>
    /// <param name="payloadType">ImGui payload type string  must match the accept types on the target.</param>
    /// <param name="payloadData">Arbitrary string payload delivered to the drop target.</param>
    /// <param name="displayLabel">Text shown in the drag tooltip. Null = use the header label.</param>
    public CollapsingHeader WithDragSource(string payloadType, string payloadData, string displayLabel = null)
    {
        var d = (CollapsingHeaderData)Data;
        d.DragPayloadType  = payloadType;
        d.DragPayloadData  = payloadData;
        d.DragDisplayLabel = displayLabel;
        return this;
    }

    /// <summary>
    /// Make the header bar a drop target with insert-between semantics: while a compatible
    /// drag hovers, an insertion line is drawn above or below the bar depending on the mouse
    /// half, and <see cref="OnDrop"/> receives (payloadType, payloadData, droppedBelow).
    /// Chainable.
    /// </summary>
    public CollapsingHeader WithDropTarget(Action<string, string, bool> onDrop, params string[] acceptTypes)
    {
        OnDrop = onDrop;
        ((CollapsingHeaderData)Data).AcceptDropTypes = acceptTypes;
        return this;
    }

    public override IEnumerable<BaseUIElement> GetChildren() => Children;

    public override void ApplyReceivedData(BaseUIElementData data)
    {
        var wasVisible = ((CollapsingHeaderData)Data).Visible;
        var wasOpen    = ((CollapsingHeaderData)Data).IsOpen;
        base.ApplyReceivedData(data);
        var nowVisible = ((CollapsingHeaderData)Data).Visible;
        var nowOpen    = ((CollapsingHeaderData)Data).IsOpen;

        if (wasVisible && !nowVisible)
            InvokeCallback(() => OnToggled?.Invoke(false));

        if (!wasOpen && nowOpen)
            InvokeCallback(() => OnToggled?.Invoke(true));

        var d = (CollapsingHeaderData)Data;
        if (d.DroppedType != null)
        {
            var type    = d.DroppedType;
            var payload = d.DroppedPayload;
            var below   = d.DroppedBelow;
            // Reset so the callback doesn't fire again on the next state report
            d.DroppedType    = null;
            d.DroppedPayload = null;
            InvokeCallback(() => OnDrop?.Invoke(type, payload, below));
        }
    }
}
