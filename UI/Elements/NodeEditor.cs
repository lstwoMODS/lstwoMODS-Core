using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class NodeEditor : BaseUIElement<NodeEditor>
{
    public List<BaseUIElement> Children;
    public List<NodeLinkData>  Links;

    public Action<int, int> OnLinkCreated;   // (startAttrId, endAttrId)
    public Action<int>      OnLinkDestroyed; // (linkId)

    public NodeEditor(string name, params BaseUIElement[] children) : base(name)
    {
        Children = new List<BaseUIElement>(children);
        Links    = new List<NodeLinkData>();
        Data = new NodeEditorData
        {
            Name     = name,
            Children = Children.Select(c => c.Data).ToList(),
            Links    = Links,
        };
    }

    public NodeEditor WithMiniMap(ImNodesMiniMapLocation location = ImNodesMiniMapLocation.BottomRight)
    { var d = (NodeEditorData)Data; d.ShowMiniMap = true; d.MiniMapLocation = location; return this; }

    public NodeEditor OnLinkCreate(Action<int, int> handler, bool mainThread = true)
    { OnLinkCreated = handler; RunCallbacksOnMainThread = mainThread; return this; }

    public NodeEditor OnLinkDestroy(Action<int> handler)
    { OnLinkDestroyed = handler; return this; }

    /// <summary>Add a link between two attribute IDs. Call this from your OnLinkCreated callback.</summary>
    public void AddLink(int id, int startAttrId, int endAttrId)
    {
        var link = new NodeLinkData { Id = id, StartAttributeId = startAttrId, EndAttributeId = endAttrId };
        Links.Add(link);
        ((NodeEditorData)Data).Links = Links;
        MarkChanged();
    }

    /// <summary>Remove a link by ID. Call this from your OnLinkDestroyed callback.</summary>
    public void RemoveLink(int id)
    {
        Links.RemoveAll(l => l.Id == id);
        ((NodeEditorData)Data).Links = Links;
        MarkChanged();
    }

    public override IEnumerable<BaseUIElement> GetChildren() => Children;

    public override void ApplyReceivedData(BaseUIElementData data)
    {
        var d = (NodeEditorData)data;
        bool linkCreated   = d.LinkCreated;
        bool linkDestroyed = d.LinkDestroyed;
        int  startAttr     = d.NewLinkStartAttr;
        int  endAttr       = d.NewLinkEndAttr;
        int  destroyedId   = d.DestroyedLinkId;
        base.ApplyReceivedData(data);

        if (linkCreated)
            InvokeCallback(() => OnLinkCreated?.Invoke(startAttr, endAttr));
        if (linkDestroyed)
            InvokeCallback(() => OnLinkDestroyed?.Invoke(destroyedId));
    }
}

public class GraphNode : BaseUIElement<GraphNode>
{
    public List<BaseUIElement> Children;

    public GraphNode(int nodeId, string title, params BaseUIElement[] children) : base($"node-{nodeId}")
    {
        Children = new List<BaseUIElement>(children);
        Data = new GraphNodeData
        {
            Name      = $"node-{nodeId}",
            NodeId    = nodeId,
            NodeTitle = title,
            Children  = Children.Select(c => c.Data).ToList()
        };
    }

    public GraphNode WithPosition(float x, float y) { var d = (GraphNodeData)Data; d.InitX = x; d.InitY = y; return this; }
    public GraphNode WithNoTitleBar() { ((GraphNodeData)Data).HasTitleBar = false; return this; }
    public override IEnumerable<BaseUIElement> GetChildren() => Children;
}

public class InputPin : BaseUIElement<InputPin>
{
    public List<BaseUIElement> Children;

    public InputPin(int attributeId, ImNodesPinShape shape = ImNodesPinShape.CircleFilled, params BaseUIElement[] children)
        : base($"inpin-{attributeId}")
    {
        Children = new List<BaseUIElement>(children);
        Data = new InputAttributeData
        {
            Name = $"inpin-{attributeId}", AttributeId = attributeId, PinShape = shape,
            Children = Children.Select(c => c.Data).ToList()
        };
    }
    public override IEnumerable<BaseUIElement> GetChildren() => Children;
}

public class OutputPin : BaseUIElement<OutputPin>
{
    public List<BaseUIElement> Children;

    public OutputPin(int attributeId, ImNodesPinShape shape = ImNodesPinShape.CircleFilled, params BaseUIElement[] children)
        : base($"outpin-{attributeId}")
    {
        Children = new List<BaseUIElement>(children);
        Data = new OutputAttributeData
        {
            Name = $"outpin-{attributeId}", AttributeId = attributeId, PinShape = shape,
            Children = Children.Select(c => c.Data).ToList()
        };
    }
    public override IEnumerable<BaseUIElement> GetChildren() => Children;
}

public class StaticPin : BaseUIElement<StaticPin>
{
    public List<BaseUIElement> Children;

    public StaticPin(int attributeId, params BaseUIElement[] children)
        : base($"staticpin-{attributeId}")
    {
        Children = new List<BaseUIElement>(children);
        Data = new StaticAttributeData
        {
            Name = $"staticpin-{attributeId}", AttributeId = attributeId,
            Children = Children.Select(c => c.Data).ToList()
        };
    }
    public override IEnumerable<BaseUIElement> GetChildren() => Children;
}
