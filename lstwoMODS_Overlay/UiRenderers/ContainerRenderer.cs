using System.Collections.Generic;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class ContainerRenderer : UIRenderer
{
    private List<BaseUIElementData> _children;

    public ContainerRenderer(BaseUIElementData data) : base(data)
    {
        _children = ((ContainerData)data).Children;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (ContainerData)data;
        Data = d;
        Name = d.Name;
        if (d.Children?.Count > 0) _children = d.Children;
    }

    public override void Render()
    {
        foreach (var child in _children)
            Window.RenderSingleElement(child);
    }

    public override BaseUIElementData? GetNewState() => null;
}
