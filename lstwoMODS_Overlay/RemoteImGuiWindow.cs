using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay;

public class RemoteImGuiWindow : NormalImGuiWindow
{
    public List<BaseUiElement> Elements = [];
    
    public RemoteImGuiWindow(string windowId, Action onConfigure, Action onRender, string windowTitle, int width, int height, (float, float, float, float) clearColor = default, WindowType type = WindowType.Normal, string iconPath = "") 
        : base(windowId, onConfigure, onRender, windowTitle, width, height, clearColor, type, iconPath)
    {
        _onRender += OnRender;
        _onConfigure += OnConfigure;
    }

    public virtual unsafe void OnConfigure()
    {
        var io = ImGui.GetIO();
        io.Fonts.AddFontFromFileTTF("Assets/InterVariable.ttf", 18);
    }

    public virtual void OnRender()
    {
        ImGui.DockSpaceOverViewport(ImGuiDockNodeFlags.PassthruCentralNode);
        
        foreach (var baseElement in Elements)
        {
            if (baseElement is DemoWindow demoWindow)
            {
                ImGui.ShowDemoWindow();
            }
        }
    }
}