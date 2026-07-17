using System;
using lstwoMODS_Core.UI.Elements;
using lstwoMODS.ImGui.Shared;

namespace lstwoMODS_Core.UI;

public class LstwoModsOverlay(IntPtr windowHandle) : OSWindow("lstwoMODS", 1280, 720, WindowType.Overlay, windowHandle, UIManager.BackendEntry?.Value)
{
    public static Action OnConstructUI;

    public LstwoModsPanels LstwoModsPanels;

    public override void ConstructUI()
    {
        Config = new ImGuiConfig
        {
            ConfigFlags = ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.DockingEnable | ImGuiConfigFlags.ViewportsEnable | ImGuiConfigFlags.NavEnableGamepad,
            ConfigWindowsResizeFromEdges = true,
            FontGlobalScale = Plugin.FontScaleEntry.Value,
        };
        
        AddFont("inter", "Assets/InterVariable.ttf", 16f);
        AddElement(LstwoModsPanels = new LstwoModsPanels());
        
        OnConstructUI?.Invoke();
    }
}
