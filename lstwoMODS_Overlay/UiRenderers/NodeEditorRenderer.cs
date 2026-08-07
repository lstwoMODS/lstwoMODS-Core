using System.Collections.Generic;
using Hexa.NET.ImNodes;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class NodeEditorRenderer : UIRenderer
{
    private List<BaseUIElementData> _children;
    private List<NodeLinkData> _links;
    private bool _showMiniMap;
    private ImNodesMiniMapLocation _miniMapLoc;
    private bool _linkCreated;
    private int _newLinkStartAttr, _newLinkEndAttr;
    private bool _linkDestroyed;
    private int _destroyedLinkId;

    public NodeEditorRenderer(BaseUIElementData data) : base(data) { CopyFrom((NodeEditorData)data); }

    private void CopyFrom(NodeEditorData d)
    {
        _children   = d.Children;
        _links      = d.Links;
        _showMiniMap = d.ShowMiniMap;
        _miniMapLoc = (ImNodesMiniMapLocation)(int)d.MiniMapLocation;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (NodeEditorData)data;
        var prev = _children;
        Data = d;
        Name = d.Name;
        CopyFrom(d);
        if (!(d.Children?.Count > 0)) _children = prev;
    }

    public override void Render()
    {
        ImNodes.BeginNodeEditor();

        foreach (var child in _children)
            Window.RenderSingleElement(child);

        if (_links != null)
            foreach (var link in _links)
                ImNodes.Link(link.Id, link.StartAttributeId, link.EndAttributeId);

        if (_showMiniMap)
            ImNodes.MiniMap(0.2f, _miniMapLoc);

        ImNodes.EndNodeEditor();

        // Detect link creation
        int startAttr = 0, endAttr = 0;
        if (ImNodes.IsLinkCreated(ref startAttr, ref endAttr))
        {
            _linkCreated       = true;
            _newLinkStartAttr  = startAttr;
            _newLinkEndAttr    = endAttr;
        }

        // Detect link destruction
        var destroyedId = 0;
        if (ImNodes.IsLinkDestroyed(ref destroyedId))
        {
            _linkDestroyed  = true;
            _destroyedLinkId = destroyedId;
        }
    }

    public override BaseUIElementData? GetNewState()
    {
        if (!_linkCreated && !_linkDestroyed) return null;

        var d = (NodeEditorData)Data;
        var result = new NodeEditorData
        {
            Id              = Data.Id,
            Name            = Data.Name,
            Enabled         = Data.Enabled,
            Children        = _children,
            Links           = _links,
            ShowMiniMap     = _showMiniMap,
            MiniMapLocation = d.MiniMapLocation
        };

        if (_linkCreated)
        {
            result.LinkCreated      = true;
            result.NewLinkStartAttr = _newLinkStartAttr;
            result.NewLinkEndAttr   = _newLinkEndAttr;
            _linkCreated = false;
        }

        if (_linkDestroyed)
        {
            result.LinkDestroyed   = true;
            result.DestroyedLinkId = _destroyedLinkId;
            _linkDestroyed = false;
        }

        return result;
    }
}
