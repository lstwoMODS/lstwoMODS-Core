using System.Collections.Generic;
using System.Linq;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

/// <summary>
/// Wraps child elements and makes them a drag source.
/// The whole bounding box of the children becomes draggable.
/// </summary>
public class DragSource : BaseUIElement<DragSource>
{
    public List<BaseUIElement> Children;

    /// <param name="name">Unique element ID.</param>
    /// <param name="payloadType">ImGui payload type string. Must match the AcceptTypes on the target.</param>
    /// <param name="payloadData">Arbitrary string payload sent to the drop target on drop.</param>
    /// <param name="children">Visual content inside the drag source area.</param>
    public DragSource(string name, string payloadType, string payloadData, params BaseUIElement[] children) : base(name)
    {
        Children = new List<BaseUIElement>(children);
        Data = new DragSourceData
        {
            Name        = name,
            PayloadType = payloadType,
            PayloadData = payloadData,
            Children    = Children.Select(c => c.Data).ToList()
        };
    }

    /// <summary>Override the text shown in the drag tooltip. Defaults to the payload type string. Chainable.</summary>
    public DragSource WithDisplayLabel(string label)
    {
        ((DragSourceData)Data).DisplayLabel = label;
        return this;
    }

    public override System.Collections.Generic.IEnumerable<BaseUIElement> GetChildren() => Children;
}
