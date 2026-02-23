using Hexa.NET.GLFW;
using Hexa.NET.OpenGL;
using lstwoMODS.ImGui.Shared;

namespace lstwoMODS_Overlay;

public class EmptyNormalWindow : GlfwWindow
{
    private Action _postInitialize;
    
    private float _r = 0.1f;
    private float _g = 0.1f;
    private float _b = 0.1f;
    private float _a = 1.0f;

    private double _lastInputTime;

    public EmptyNormalWindow(Action postInitialize, (float, float, float, float) clearColor, WindowType type, string windowTitle, int width, int height) : base(clearColor, type, windowTitle, width, height)
    {
        _postInitialize = postInitialize;
    }

    protected override bool InitializeImGui()
    {
        return true;
    }

    protected override void ShutdownImGui()
    {
    }

    protected override unsafe void OnPreFirstFrame()
    {
        GLFW.SetCursorPosCallback(GlfwWindowPtr, OnMouseMove);
        GLFW.SetMouseButtonCallback(GlfwWindowPtr, OnMouseButton);
        GLFW.SetScrollCallback(GlfwWindowPtr, OnScroll);

        _postInitialize();
    }

    protected override void OnIfMinimized()
    {
    }

    protected override void RenderFrame()
    {
    }

    protected override float GetMainScale()
    {
        return 1.0f;
    }

    protected override bool HasVisibleContent()
    {
        return false;
    }

    protected override void BeginFrame()
    {
        GLFW.MakeContextCurrent(GlfwWindowPtr);
        
        if (GLFW.GetTime() - _lastInputTime > 0.2)
        {
            _r = Lerp(_r, 0.1f, 0.1f);
            _g = Lerp(_g, 0.1f, 0.1f);
            _b = Lerp(_b, 0.1f, 0.1f);
        }

        GL.ClearColor(_r, _g, _b, _a);
        GL.Clear(GLClearBufferMask.ColorBufferBit);
    }
    
    float Lerp(float firstFloat, float secondFloat, float by)
    {
        return firstFloat * (1 - by) + secondFloat * by;
    }

    private void Touch()
    {
        _lastInputTime = GLFW.GetTime();
    }

    private unsafe void OnMouseMove(nint window1, double x, double y)
    {
        _r = 0.2f;
        _g = 0.4f;
        _b = 1.0f; // blue
        Touch();
    }

    private unsafe void OnMouseButton(nint window1, int button, int action, int mods)
    {
        if (action != GLFW.GLFW_PRESS)
            return;

        if (button == 0)
        {
            _r = 1.0f; _g = 0.2f; _b = 0.2f; // red
        }
        else if (button == 1)
        {
            _r = 0.2f; _g = 1.0f; _b = 0.2f; // green
        }

        Touch();
    }

    private unsafe void OnScroll(nint window1, double x, double y)
    {
        _r = 1.0f;
        _g = 1.0f;
        _b = 0.2f; // yellow
        Touch();
    }
}