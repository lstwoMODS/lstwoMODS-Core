using lstwoMODS_Core.UI.Elements;

namespace lstwoMODS_Core.UI.TabMenus;

public class UIInspectorWindow : BaseWindow
{
    public UIInspectorWindow()
    {
        Name = "UI Inspector";
        TitleIcon = Lucide.ScanSearch;
    }

    public override Group ConstructUI()
    {
        return new Group("UIInspector", new UIInspector("UIInspector"));
    }

    public override void RefreshUI() { }
}
