using System.Collections.Generic;
using System.Linq;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

/// <summary>
/// A logical container that renders children inside ImGui.BeginGroup()/EndGroup().
/// Supports all push/pop style commands via the inherited WithStyleVar, WithStyleColor, WithFont, WithId, and WithPreset methods.
/// </summary>
public class Group : BaseUIElement<Group>
{
    public List<BaseUIElement> Children { get; }

    public Group(string name, params BaseUIElement[] children) : base(name)
    {
        Children = new List<BaseUIElement>(children);

        Data = new GroupData
        {
            Name     = name,
            Children = Children.Select(c => c.Data).ToList()
        };
    }

    public override IEnumerable<BaseUIElement> GetChildren() => Children;

    /// <summary>
    /// Reorder this group's existing children. Pass the same child elements in their new
    /// order  children that are omitted keep their relative order at the end. Does not add
    /// or remove children. The order reaches the overlay through GroupData.OrderedIds
    /// (plain Children edits don't sync  UpdatedElements are serialized without children).
    /// Calls MarkChanged().
    /// </summary>
    public void SetChildOrder(IEnumerable<BaseUIElement> orderedChildren)
    {
        var ordered = orderedChildren.Where(Children.Contains).ToList();
        ordered.AddRange(Children.Where(c => !ordered.Contains(c)));

        Children.Clear();
        Children.AddRange(ordered);

        var data = (GroupData)Data;
        data.Children   = Children.Select(c => c.Data).ToList();
        data.OrderedIds = Children.Select(c => c.Data.Id).ToArray();
        MarkChanged();
    }
}
