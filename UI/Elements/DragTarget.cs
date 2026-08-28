using System;
using System.Collections.Generic;
using System.Linq;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

/// <summary>
/// Wraps child elements and makes their bounding box a drop target.
/// Fires <see cref="OnDrop"/> when a compatible drag source is released over it.
/// </summary>
public class DragTarget : BaseUIElement<DragTarget>
{
    public List<BaseUIElement> Children;
    /// <summary>Fired when a drop completes. Parameters: (payloadType, payloadData).</summary>
    public Action<string, string> OnDrop;
    /// <summary>Insert-between variant of <see cref="OnDrop"/>: (payloadType, payloadData, after)
    /// where <c>after</c> is true when the drop landed on the right half (insert after this
    /// element). Set via <see cref="WithInsertBetween"/>; takes precedence over <see cref="OnDrop"/>.</summary>
    public Action<string, string, bool> OnDropBetween;

    /// <param name="name">Unique element ID.</param>
    /// <param name="onDrop">Callback receiving (payloadType, payloadData) on a successful drop.</param>
    /// <param name="acceptTypes">Payload type strings this target accepts.</param>
    /// <param name="children">Visual content shown inside the drop zone.</param>
    public DragTarget(string name, Action<string, string> onDrop, string[] acceptTypes, params BaseUIElement[] children) : base(name)
    {
        Children = new List<BaseUIElement>(children);
        OnDrop   = onDrop;
        Data = new DragTargetData
        {
            Name        = name,
            AcceptTypes = acceptTypes ?? Array.Empty<string>(),
            Children    = Children.Select(c => c.Data).ToList()
        };
    }

    /// <summary>Switch to insert-between mode: the overlay draws an insertion line at the edge of
    /// whichever half the cursor is over instead of a whole-element highlight, and reports the side
    /// so a drop lands in the gap between items. <paramref name="onDropBetween"/> receives
    /// (payloadType, payloadData, after).
    ///
    /// <paramref name="vertical"/> splits the element top and bottom rather than left and right,
    /// which is what a list stacked down the screen needs; the default suits a row or a grid.
    /// Chainable.</summary>
    public DragTarget WithInsertBetween(Action<string, string, bool> onDropBetween, bool vertical = false)
    {
        var d = (DragTargetData)Data;
        d.InsertBetween  = true;
        d.InsertVertical = vertical;
        OnDropBetween = onDropBetween;
        return this;
    }

    public override IEnumerable<BaseUIElement> GetChildren() => Children;

    public override void ApplyReceivedData(BaseUIElementData data)
    {
        base.ApplyReceivedData(data);
        var d = (DragTargetData)Data;
        if (d.DroppedType != null)
        {
            var type    = d.DroppedType;
            var payload = d.DroppedPayload;
            var after   = d.DroppedAfter;
            // Reset so the callback doesn't fire again next frame
            d.DroppedType    = null;
            d.DroppedPayload = null;
            if (OnDropBetween != null) InvokeCallback(() => OnDropBetween(type, payload, after));
            else                       InvokeCallback(() => OnDrop?.Invoke(type, payload));
        }
    }
}
