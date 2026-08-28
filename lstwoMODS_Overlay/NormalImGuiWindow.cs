using System.Numerics;
using System.Runtime.CompilerServices;
using Hexa.NET.GLFW;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.GLFW;
using Hexa.NET.ImGuizmo;
using lstwoMODS.ImGui.Shared;
using lstwoMODS_Overlay.Backends;
using GLFWmonitorPtr = Hexa.NET.GLFW.GLFWmonitorPtr;
using GLFWwindowPtr = Hexa.NET.GLFW.GLFWwindowPtr;
using ImGuiConfigFlags = Hexa.NET.ImGui.ImGuiConfigFlags;
using ImGuiCol = Hexa.NET.ImGui.ImGuiCol;
using static lstwoMODS_Overlay.Logger;

namespace lstwoMODS_Overlay;

public class NormalImGuiWindow : GlfwWindow
{
    private readonly string _windowId;
    protected Action _onConfigure;
    protected Action _onRender;

    private ImGuiContextPtr _imGuiContext;
    private ImGuiIOPtr _io;

    public NormalImGuiWindow(string windowId, Action onConfigure, Action onRender, string windowTitle, int width, int height, (float, float, float, float) clearColor = default, WindowType type = WindowType.Normal, string iconPath = "", bool allowClose = false, IRenderBackend? backend = null)
        : base(clearColor == default ? (0.45f, 0.55f, 0.60f, 1.00f) : clearColor, type, windowTitle, width, height, iconPath, allowClose, backend)
    {
        _windowId = windowId;
        _onConfigure = onConfigure;
        _onRender = onRender;
    }

    public ImGuiContextPtr GetImGuiContext()
    {
        return _imGuiContext;
    }
    
    protected override bool InitializeImGui()
    {
        _imGuiContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(_imGuiContext);

        _io = ImGui.GetIO();
        _io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        _io.ConfigFlags |= ImGuiConfigFlags.NavEnableGamepad;
        _io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        _io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;

        ImGui.StyleColorsDark();
        var style = ImGui.GetStyle();
        style.ScaleAllSizes(MainScale);
        style.FontScaleDpi = MainScale;
        _io.ConfigDpiScaleFonts = true;
        _io.ConfigDpiScaleViewports = true;
        
        _onConfigure();

        style.Colors[(int)ImGuiCol.DockingEmptyBg] = new Vector4(0.0f);
        
        if ((_io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
        {
            style.WindowRounding = 0.0f;
            style.Colors[(int)ImGuiCol.WindowBg].W = 1.0f;
        }
        
        ImGuiImplGLFW.SetCurrentContext(_imGuiContext);
        var glfwWindow = Unsafe.As<GLFWwindowPtr, Hexa.NET.ImGui.Backends.GLFW.GLFWwindowPtr>(ref GlfwWindowPtr);
        var glfwOk = Backend.IsOpenGL
            ? ImGuiImplGLFW.InitForOpenGL(glfwWindow, true)
            : ImGuiImplGLFW.InitForOther(glfwWindow, true);

        if (!glfwOk)
        {
            Console.WriteLine("Failed to init ImGui Impl GLFW");
            return false;
        }

        if (!Backend.InitImGuiRenderer())
        {
            Console.WriteLine("Failed to init ImGui renderer backend");
            return false;
        }

        return true;
    }

    protected override void ShutdownImGui()
    {
        Backend.ShutdownImGuiRenderer();
        ImGuiImplGLFW.Shutdown();
        ImGuiImplGLFW.SetCurrentContext(null);
        ImGui.DestroyContext();
    }

    protected override void OnPreFirstFrame() { }

    protected override void OnIfMinimized()
    {
        ImGuiImplGLFW.Sleep(10);
    }

    protected override bool HasVisibleContent()
    {
        try
        {
            var dd = _lastDrawData;

            if (dd.CmdListsCount == 0)
                return false;

            for (var i = 0; i < dd.CmdListsCount; i++)
            {
                var list = dd.CmdLists[i];
                if (list.CmdBuffer.Size > 0)
                    return true;
            }
        }
        catch
        {
            // ignored
        }

        return false;
    }
    
    private ImDrawDataPtr _lastDrawData;

    protected override void RenderFrame()
    {
        Backend.NewImGuiFrame();
        ImGuiImplGLFW.NewFrame();

        // Backstop for the frames after a pass-through transition, before SyncMainViewportInputFlag
        // below has been seen by the backend: until then NewFrame still resets
        // GLFW_MOUSE_PASSTHROUGH and strips WS_EX_TRANSPARENT off the overlay. Also covers any
        // future path that clears the styles behind our back.
        EnforceInputPassthrough();

        ImGui.NewFrame();

        // Must come after NewFrame, which rewrites the main viewport's flags.
        SyncMainViewportInputFlag();

        try
        {
            _onRender();
        }
        catch (Exception ex)
        {
            CrashGuard.Report("frame render", ex);
        }

        ImGui.Render();

        _lastDrawData = ImGui.GetDrawData();
        Backend.RenderImGuiDrawData(_lastDrawData);

        if ((_io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
        {
            ImGui.UpdatePlatformWindows();
            ImGui.RenderPlatformWindowsDefault();
        }
    }

    protected override float GetMainScale()
    {
        var mon = GLFW.GetPrimaryMonitor();
        return ImGuiImplGLFW.GetContentScaleForMonitor(Unsafe.As<GLFWmonitorPtr, Hexa.NET.ImGui.Backends.GLFW.GLFWmonitorPtr>(ref mon));
    }

}