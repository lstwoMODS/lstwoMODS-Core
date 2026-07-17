using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class UIInspector : BaseUIElement<UIInspector>
{
    public UIInspector(string name = "UIInspector") : base(name)
    {
        Data = new UIInspectorData { Name = name };
    }

    public override void ApplyReceivedData(BaseUIElementData data)
    {
        base.ApplyReceivedData(data);
    }
}
