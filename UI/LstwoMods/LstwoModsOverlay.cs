using System;
using lstwoMODS_Core.UI.Elements;
using lstwoMODS.ImGui.Shared;

namespace lstwoMODS_Core.UI;

public class LstwoModsOverlay(IntPtr windowHandle) : OSWindow("lstwoMODS", 1280, 720, ResolveWindowType(), windowHandle, UIManager.BackendEntry?.Value)
{
    public static Action OnConstructUI;

    public LstwoModsPanels LstwoModsPanels;

    private static WindowType ResolveWindowType()
        => (Plugin.MainViewportSeparateWindowEntry?.Value ?? false)
            ? WindowType.Normal
            : WindowType.Overlay;

    public static float[] ParseBackgroundColor(string hex)
        => !string.IsNullOrEmpty(hex) && UnityEngine.ColorUtility.TryParseHtmlString(hex, out var c)
            ? new[] { c.r, c.g, c.b, c.a }
            : new[] { 0.45f, 0.55f, 0.60f, 1.0f };

    public override void ConstructUI()
    {
        var configFlags = ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.DockingEnable | ImGuiConfigFlags.NavEnableGamepad;
        if (!(Plugin.DisableMultiViewportEntry?.Value ?? false))
            configFlags |= ImGuiConfigFlags.ViewportsEnable;

        Config = new ImGuiConfig
        {
            ConfigFlags = configFlags,
            ConfigWindowsResizeFromEdges = true,
            FontGlobalScale = Plugin.FontScaleEntry.Value,
            BackgroundMode = Plugin.WindowBackgroundModeEntry?.Value ?? ImGuiConfig.WindowBackgroundMode.MatchImGui,
            WindowBackgroundColor = ParseBackgroundColor(Plugin.WindowBackgroundColorEntry?.Value),
        };
        
        AddFont("inter", "Assets/InterVariable.ttf", 16f);
        AddElement(LstwoModsPanels = new LstwoModsPanels());
        
        OnConstructUI?.Invoke();
    }
}
