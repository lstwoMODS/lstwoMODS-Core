using System.Numerics;
using Hexa.NET.GLFW;
using Hexa.NET.ImGui;
using Hexa.NET.OpenGL;
using lstwoMODS.ImGui.Shared;
using GLFWwindowPtr    = Hexa.NET.GLFW.GLFWwindowPtr;
using ImGuiWindowFlags   = Hexa.NET.ImGui.ImGuiWindowFlags;
using ImGuiDockNodeFlags = Hexa.NET.ImGui.ImGuiDockNodeFlags;

namespace lstwoMODS_Overlay;

internal class TestProgram
{
    public static bool shouldClose = false;
    
    private static ImFontPtr font;
    private static NormalImGuiWindow _window;
    private static EmptyNormalWindow _testWindow;
    
    public static void _Main(string[] args)
    {
        _testWindow = new EmptyNormalWindow(() =>
        {
            _window = new NormalImGuiWindow("", Style, ImGuiFrame, "test", 1280, 720, (0,0,0,0), WindowType.Overlay);
            _window.StartThread();
            _window.TrackWindow(GLFW.GetWin32Window(_testWindow.GlfwWindowPtr));
        }, (0f, 0.85f, 1f, 1f), WindowType.Normal, "test", 1280, 720);
        _testWindow.StartThread();
        
        GLFW.Terminate();
    }

    private static float testFloat;
    private static Vector3 testColor;
    
    private static void ImGuiFrame()
    {
        ImGui.DockSpaceOverViewport(ImGui.GetMainViewport(), ImGuiDockNodeFlags.PassthruCentralNode);

        if (ImGui.Begin("Player Mods###PlayerMods", ImGuiWindowFlags.None))
        {
            if (ImGui.CollapsingHeader($"Test Player Mod###TestPlayerMod"))
            {
                ImGui.Text($"test");
            }
            
            if (ImGui.CollapsingHeader($"Another Test Player Mod###AnotherTestPlayerMod"))
            {
                ImGui.Text($"test");
                ImGui.Separator();
                ImGui.DragFloat("test float", ref testFloat);
            }
        }
        
        ImGui.End();
        
        if (ImGui.Begin("Server Mods###ServerMods", ImGuiWindowFlags.None))
        {
            if (ImGui.CollapsingHeader($"Test Server Mod###TestServerMod"))
            {
                ImGui.Text($"test");
                ImGui.Separator();
                ImGui.DragFloat("test float", ref testFloat);
            }
            
            if (ImGui.CollapsingHeader($"Another Test Server Mod###AnotherTestServerMod"))
            {
                ImGui.Text($"test");
                ImGui.Separator();
                ImGui.ColorEdit3("test color", ref testColor);
            }
        }
        
        ImGui.End();
    }

    private static unsafe void Style()
    {
        var style = ImGui.GetStyle();
        
        style.WindowMinSize = new Vector2(10, 10);
        style.Alpha = 1.0f;
        style.WindowPadding = new(8, 8);
        style.WindowRounding = 8;
        style.WindowBorderSize = 1;
        style.ChildRounding = 6;
        style.ChildBorderSize = 1;
        style.PopupRounding = 8;
        style.PopupBorderSize = 1;
        style.FramePadding = new(4, 3);
        style.FrameRounding = 6;
        style.FrameBorderSize = 0;
        style.ItemSpacing = new(8, 4);
        style.ItemInnerSpacing = new(4, 4);
        style.CellPadding = new(0, 0);
        style.TouchExtraPadding = new(0, 0);
        style.ScrollbarRounding = 9;
        style.TabRounding = 6;
        style.TabBorderSize = 0;
        style.DisplayWindowPadding = new(19, 19);
        style.AntiAliasedLines = true;
        style.AntiAliasedFill = true;
        style.AntiAliasedLinesUseTex = true;
        style.CurveTessellationTol = 1.12f;
        style.CircleTessellationMaxError = 0.3f;
        
        style.Colors[0] = new(1, 1, 1, 1);
        //style.Colors[2] = new(0.295f, 0.308f, 0.333f, 1);
        style.Colors[3] = new(0.09411765f, 0.109803922f, 0.13333334f, 1);
        style.Colors[4] = new(0.0784313753f, 0.0784313753f, 0.0784313753f, 1);
        style.Colors[5] = new(0.431372583f, 0.431372583f, 0.5019608f, 0.545098066f);
        style.Colors[21] = new(0.258823544f, 0.5882353f, 0.9803922f, 0.3529412f);
        style.Colors[22] = new(0.258823544f, 0.5882353f, 0.9803922f, 1.0f);
        style.Colors[23] = new(0.0588235334f, 0.5294118f, 0.9803922f, 1.0f);
        
        var io = ImGui.GetIO();

        font = io.Fonts.AddFontFromFileTTF("Assets/InterVariable.ttf", 16f, io.Fonts.GetGlyphRangesDefault());
    }
}