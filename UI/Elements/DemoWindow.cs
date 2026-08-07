using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class DemoWindow : BaseUIElement<DemoWindow>
{
    public DemoWindow(string name) : base(name)
    {
        Data = new DemoWindowData
        {
            Name = name,
            Enabled = true
        };
    }
}