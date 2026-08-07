using System;
using System.Collections.Generic;
using System.Linq;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class TreeNode : BaseUIElement<TreeNode>
{
    public List<BaseUIElement> Children;
    /// <summary>Elements on the node's line (see <see cref="WithLineElements"/>).</summary>
    public List<BaseUIElement> LineElements = new();
    public Action<bool>? OnToggled;
    /// <summary>
    /// Fired when a compatible payload is dropped on the node line.
    /// Parameters: (payloadType, payloadData, droppedBelow)  droppedBelow is true when the
    /// drop landed on the lower half of the line (insert after). See <see cref="WithDropTarget"/>.
    /// </summary>
    public Action<string, string, bool>? OnDrop;

    public string Label
    {
        get => ((TreeNodeData)Data).Label;
        set { ((TreeNodeData)Data).Label = value; MarkChanged(); }
    }

    public TreeNode(string name, string label, params BaseUIElement[] children) : base(name)
    {
        Children = new List<BaseUIElement>(children);
        Data = new TreeNodeData
        {
            Name     = name,
            Label    = label,
            Children = Children.Select(c => c.Data).ToList()
        };
    }

    public TreeNode DefaultOpen()  { ((TreeNodeData)Data).Flags |= ImGuiTreeNodeFlags.DefaultOpen; return this; }
    public TreeNode WithFlags(ImGuiTreeNodeFlags flags) { ((TreeNodeData)Data).Flags = flags; return this; }
    public TreeNode OnToggle(Action<bool> cb, bool mainThread = true) { OnToggled = cb; RunCallbacksOnMainThread = mainThread; return this; }

    /// <summary>
    /// Put ordinary elements (small buttons, text, ...) on the node's line after the label.
    /// Unlike children they stay visible while the node is collapsed; their callbacks work
    /// as normal. Chainable.
    /// </summary>
    public TreeNode WithLineElements(params BaseUIElement[] elements)
    {
        LineElements.AddRange(elements);
        ((TreeNodeData)Data).LineChildren = LineElements.Select(e => e.Data).ToList();
        return this;
    }

    /// <summary>Pin the line elements to the right edge of the node line instead of
    /// flowing after the label. Chainable.</summary>
    public TreeNode PinLineElementsEnd() { ((TreeNodeData)Data).PinLineChildrenEnd = true; return this; }

    /// <summary>
    /// Dimmed text between the label and the (pinned) line elements. The overlay
    /// auto-ellipsizes it to the available width, so it can never overflow the line;
    /// <paramref name="tooltip"/> shows the untruncated form on hover. Chainable.
    /// </summary>
    public TreeNode WithLineTag(string tag, string tooltip = null)
    {
        var d = (TreeNodeData)Data;
        d.LineTag = tag;
        d.LineTagTooltip = tooltip;
        return this;
    }

    /// <summary>
    /// Make the node line an ImGui drag source (the open body stays interactive as normal).
    /// Click-to-expand keeps working  a drag only starts past the mouse drag threshold. Chainable.
    /// </summary>
    public TreeNode WithDragSource(string payloadType, string payloadData, string displayLabel = null)
    {
        var d = (TreeNodeData)Data;
        d.DragPayloadType  = payloadType;
        d.DragPayloadData  = payloadData;
        d.DragDisplayLabel = displayLabel;
        return this;
    }

    /// <summary>
    /// Make the node line a drop target with insert-between semantics: while a compatible
    /// drag hovers, an insertion line is drawn above or below the line depending on the mouse
    /// half, and <see cref="OnDrop"/> receives (payloadType, payloadData, droppedBelow). Chainable.
    /// </summary>
    public TreeNode WithDropTarget(Action<string, string, bool> onDrop, params string[] acceptTypes)
    {
        OnDrop = onDrop;
        ((TreeNodeData)Data).AcceptDropTypes = acceptTypes;
        return this;
    }

    public override IEnumerable<BaseUIElement> GetChildren() => Children.Concat(LineElements);

    public override void ApplyReceivedData(BaseUIElementData data)
    {
        base.ApplyReceivedData(data);
        var d = (TreeNodeData)Data;

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
