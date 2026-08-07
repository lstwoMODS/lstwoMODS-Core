using System.Collections.Generic;
using System.Numerics;
using Hexa.NET.ImGui;
using Hexa.NET.ImNodes;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class GraphNodeRenderer : UIRenderer
{
    private int _nodeId;
    private string _title;
    private bool _hasTitleBar;
    private float? _initX, _initY;
    private bool _positioned;
    private List<BaseUIElementData> _children;

    public GraphNodeRenderer(BaseUIElementData data) : base(data) { CopyFrom((GraphNodeData)data); }

    private void CopyFrom(GraphNodeData d)
    {
        _nodeId      = d.NodeId;
        _title       = d.NodeTitle;
        _hasTitleBar = d.HasTitleBar;
        _initX       = d.InitX;
        _initY       = d.InitY;
        _children    = d.Children;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (GraphNodeData)data;
        var prev = _children;
        Data = d;
        Name = d.Name;
        CopyFrom(d);
        if (!(d.Children?.Count > 0)) _children = prev;
    }

    public override void Render()
    {
        if (!_positioned && _initX.HasValue && _initY.HasValue)
        {
            ImNodes.SetNodeEditorSpacePos(_nodeId, new Vector2(_initX.Value, _initY.Value));
            _positioned = true;
        }

        ImNodes.BeginNode(_nodeId);

        if (_hasTitleBar)
        {
            ImNodes.BeginNodeTitleBar();
            ImGui.Text(_title);
            ImNodes.EndNodeTitleBar();
        }

        foreach (var child in _children)
            Window.RenderSingleElement(child);

        ImNodes.EndNode();
    }

    public override BaseUIElementData? GetNewState() => null;
}
