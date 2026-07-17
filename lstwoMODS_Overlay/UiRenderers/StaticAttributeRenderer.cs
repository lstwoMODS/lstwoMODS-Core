using System.Collections.Generic;
using Hexa.NET.ImNodes;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class StaticAttributeRenderer : UIRenderer
{
    private int _attrId;
    private List<BaseUIElementData> _children;

    public StaticAttributeRenderer(BaseUIElementData data) : base(data) { CopyFrom((StaticAttributeData)data); }

    private void CopyFrom(StaticAttributeData d)
    {
        _attrId   = d.AttributeId;
        _children = d.Children;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (StaticAttributeData)data;
        var prev = _children;
        Data = d;
        Name = d.Name;
        CopyFrom(d);
        if (!(d.Children?.Count > 0)) _children = prev;
    }

    public override void Render()
    {
        ImNodes.BeginStaticAttribute(_attrId);
        foreach (var child in _children)
            Window.RenderSingleElement(child);
        ImNodes.EndStaticAttribute();
    }

    public override BaseUIElementData? GetNewState() => null;
}
