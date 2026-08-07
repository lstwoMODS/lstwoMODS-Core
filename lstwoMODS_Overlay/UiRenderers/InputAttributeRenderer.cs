using System.Collections.Generic;
using Hexa.NET.ImNodes;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class InputAttributeRenderer : UIRenderer
{
    private int _attrId;
    private ImNodesPinShape _shape;
    private List<BaseUIElementData> _children;

    public InputAttributeRenderer(BaseUIElementData data) : base(data) { CopyFrom((InputAttributeData)data); }

    private void CopyFrom(InputAttributeData d)
    {
        _attrId   = d.AttributeId;
        _shape    = (ImNodesPinShape)(int)d.PinShape;
        _children = d.Children;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (InputAttributeData)data;
        var prev = _children;
        Data = d;
        Name = d.Name;
        CopyFrom(d);
        if (!(d.Children?.Count > 0)) _children = prev;
    }

    public override void Render()
    {
        ImNodes.BeginInputAttribute(_attrId, _shape);
        foreach (var child in _children)
            Window.RenderSingleElement(child);
        ImNodes.EndInputAttribute();
    }

    public override BaseUIElementData? GetNewState() => null;
}
