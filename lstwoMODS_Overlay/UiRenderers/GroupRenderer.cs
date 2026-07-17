using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class GroupRenderer : UIRenderer
{
    private List<BaseUIElementData> _children;

    public GroupRenderer(BaseUIElementData data) : base(data)
    {
        _children = ((GroupData)data).Children;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var groupData = (GroupData)data;
        Data = groupData;
        Name = groupData.Name;
        if (groupData.Children?.Count > 0) _children = groupData.Children;

        // OrderedIds survives the shallow update path (Children does not), so reorder the
        // children we already have. Ids we don't know keep their relative order at the end.
        // Reorder IN PLACE: renderers must keep holding the data's child list by reference,
        // so runtime-created/removed children (FrameState CreatedElements/RemovedElementIds
        // splicing into that list) stay visible without another update round-trip.
        if (groupData.OrderedIds is { Length: > 0 })
        {
            var byId = _children.ToDictionary(c => c.Id);
            var ordered = groupData.OrderedIds
                .Where(byId.ContainsKey)
                .Select(id => byId[id])
                .ToList();
            ordered.AddRange(_children.Where(c => !groupData.OrderedIds.Contains(c.Id)));
            _children.Clear();
            _children.AddRange(ordered);
        }
    }

    public override void Render()
    {
        ImGui.BeginGroup();

        foreach (var child in _children)
        {
            Window.RenderSingleElement(child);
        }

        ImGui.EndGroup();
    }

    public override BaseUIElementData? GetNewState()
    {
        // Groups don't have state of their own  children report their own changes
        return null;
    }
}
