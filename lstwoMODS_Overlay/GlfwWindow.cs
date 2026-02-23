using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Hexa.NET.GLFW;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.GLFW;
using Hexa.NET.OpenGL;
using HexaGen.Runtime;
using lstwoMODS.ImGui.Shared;
using GLFWwindowPtr = Hexa.NET.GLFW.GLFWwindowPtr;

using static lstwoMODS_Overlay.Logger;

namespace lstwoMODS_Overlay;

public abstract class GlfwWindow : Window
{
    protected NativeCallback<GLFWerrorfun> ErrorCallback;

    protected WindowType Type;

    public GLFWwindowPtr GlfwWindowPtr;
    protected GL GL;
    protected string GlslVersion;
    protected IntPtr TargetHwnd = IntPtr.Zero;

    protected (float, float, float, float) ClearColor;
    private bool _lastTopMostState;
    private bool _lastHoverState;
    private long _lastForeground;

    private string _windowTitle;
    private string _iconPath;

    public GlfwWindow((float, float, float, float) clearColor, WindowType type, string windowTitle, int width, int height, string iconPath = "")
    {
        ClearColor = clearColor;
        Type = type;
        _windowTitle = windowTitle;
        _iconPath = iconPath;
    }
    
    public static unsafe void SetIcon(GLFWwindowPtr window, string path)
    {
        using var bitmap = new Bitmap(path);

        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        var byteCount = bitmap.Width * bitmap.Height * 4;
        var pixels = new byte[byteCount];

        Marshal.Copy(data.Scan0, pixels, 0, byteCount);
        bitmap.UnlockBits(data);

        // Convert ARGB -> RGBA (GLFW expects RGBA)
        for (var i = 0; i < pixels.Length; i += 4)
        {
            var a = pixels[i + 3];
            var r = pixels[i + 2];
            var g = pixels[i + 1];
            var b = pixels[i + 0];

            pixels[i + 0] = r;
            pixels[i + 1] = g;
            pixels[i + 2] = b;
            pixels[i + 3] = a;
        }

        fixed (byte* pixelsPtr = pixels)
        {
            var image = new Hexa.NET.GLFW.GLFWimage
            {
                Width = bitmap.Width,
                Height = bitmap.Height,
                Pixels = pixelsPtr
            };

            GLFW.SetWindowIcon(window, 1, in image);
        }
        
        var hIconSmall = LoadImage(IntPtr.Zero, path, IMAGE_ICON, 16, 16, LR_LOADFROMFILE);
        var hIconBig   = LoadImage(IntPtr.Zero, path, IMAGE_ICON, 32, 32, LR_LOADFROMFILE);
        
        IntPtr hwnd = GLFW.GetWin32Window(window);

        SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_SMALL, hIconSmall);
        SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_BIG,   hIconBig);
    }

    public void TrackWindow(IntPtr hwnd)
    {
        TargetHwnd = hwnd;
    }
    
    protected override bool CreateWindow()
    {
        unsafe
        {
            ErrorCallback = new(static (errorCode, description) =>
            {
                LogError(Utils.DecodeStringUTF8((byte*)description));
            });
            
            GLFW.SetErrorCallback(ErrorCallback);
        }

        GLFW.Init();
        GlslVersion = "#version 150";
        GLFW.WindowHint(GLFW.GLFW_CONTEXT_VERSION_MAJOR, 3);
        GLFW.WindowHint(GLFW.GLFW_CONTEXT_VERSION_MINOR, 2);
        GLFW.WindowHint(GLFW.GLFW_OPENGL_PROFILE, GLFW.GLFW_OPENGL_CORE_PROFILE);

        if (Type == WindowType.Overlay)
        {
            GLFW.WindowHint(GLFW.GLFW_TRANSPARENT_FRAMEBUFFER, GLFW.GLFW_TRUE);
            GLFW.WindowHint(GLFW.GLFW_DECORATED, GLFW.GLFW_FALSE);
        }
        else
        {
            GLFW.WindowHint(GLFW.GLFW_TRANSPARENT_FRAMEBUFFER, GLFW.GLFW_FALSE);
            GLFW.WindowHint(GLFW.GLFW_DECORATED, GLFW.GLFW_TRUE);
        }

        MainScale = GetMainScale();
        GlfwWindowPtr = GLFW.CreateWindow((int)(1280 * MainScale), (int)(800 * MainScale), _windowTitle, null, null);
        
        if (GlfwWindowPtr.IsNull)
        {
            LogError("Failed to create GLFW window.");
            GLFW.Terminate();
            return false;
        }
        
        GLFW.MakeContextCurrent(GlfwWindowPtr);

        if (!string.IsNullOrEmpty(_iconPath))
        {
            GLFW.ShowWindow(GlfwWindowPtr);
            SetIcon(GlfwWindowPtr, _iconPath);
        }
        
        return true;
    }

    protected override bool CreateGraphicsContext()
    {
        GL = new GL(new GlfwBindingsContext(GlfwWindowPtr));
        GL.Enable(GLEnableCap.Blend);
        GL.BlendFunc(GLBlendingFactor.SrcAlpha, GLBlendingFactor.OneMinusSrcAlpha);
        return true;
    }

    protected override void PollEvents()
    {
        TrackTargetWindow();
        GLFW.PollEvents();
    }

    protected override bool ShouldClose()
    {
        return GLFW.WindowShouldClose(GlfwWindowPtr) != 0;
    }

    protected override bool IsMinimized()
    {
        return GLFW.GetWindowAttrib(GlfwWindowPtr, GLFW.GLFW_ICONIFIED) != 0;
    }

    protected override void BeginFrame()
    {
        GLFW.MakeContextCurrent(GlfwWindowPtr);

        if (Type == WindowType.Overlay)
        {
            GL.ClearColor(0,0,0,0);
        }
        else
        {
            GL.ClearColor(ClearColor.Item1, ClearColor.Item2, ClearColor.Item3, ClearColor.Item4);
        }
        
        GL.Clear(GLClearBufferMask.ColorBufferBit);
    }

    protected override void EndFrame()
    {
        GLFW.MakeContextCurrent(GlfwWindowPtr);
        GLFW.SwapBuffers(GlfwWindowPtr);
    }

    protected override void DestroyGraphicsContext()
    {
        GL.Dispose();
    }

    protected override void DestroyWindow()
    {
        GLFW.DestroyWindow(GlfwWindowPtr);
        TestProgram.shouldClose = true;
    }
    
    protected void TrackTargetWindow()
    {
        if (Type != WindowType.Overlay)
            return;

        if (TargetHwnd == IntPtr.Zero || !IsWindow(TargetHwnd))
            return;

        if (IsIconic(TargetHwnd))
            return;

        if (!GetClientBounds(TargetHwnd, out var rect))
            return;

        var dpi = (uint)GetDpiForWindow(TargetHwnd);
        var scale = dpi / 96.0f;

        var physicalWidth  = rect.right - rect.left;
        var physicalHeight = rect.bottom - rect.top;

        var logicalX = (int)(rect.left / scale);
        var logicalY = (int)(rect.top / scale);
        var logicalW = (int)(physicalWidth / scale);
        var logicalH = (int)(physicalHeight / scale);

        GLFW.SetWindowPos(GlfwWindowPtr, logicalX, logicalY);
        GLFW.SetWindowSize(GlfwWindowPtr, logicalW, logicalH);

        var hwnd = GLFW.GetWin32Window(GlfwWindowPtr);
        var flags = SWP_NOMOVE | SWP_NOSIZE | SWP_NOREDRAW | SWP_NOACTIVATE;

        var hWndInsertAfter = HasVisibleContent() ? GetWindow(TargetHwnd, GW_HWNDPREV) : GetWindow(TargetHwnd, GW_HWNDNEXT);

        SetWindowPos(hwnd, hWndInsertAfter, 0, 0, 0, 0, flags);
    }
    
    private bool GetClientBounds(IntPtr hwnd, out RECT clientRect)
    {
        clientRect = default;

        if (DwmGetWindowAttribute(
                hwnd,
                DWMWA_EXTENDED_FRAME_BOUNDS,
                out var frame,
                Marshal.SizeOf<RECT>()) != 0)
            return false;

        if (!GetClientRect(hwnd, out var client))
            return false;

        var pt = new POINT { x = 0, y = 0 };
        ClientToScreen(hwnd, ref pt);

        clientRect.left   = pt.x;
        clientRect.top    = pt.y;
        clientRect.right  = pt.x + client.right;
        clientRect.bottom = pt.y + client.bottom;

        return true;
    }

    private bool _lastHovering;

    protected abstract bool HasVisibleContent();
    
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left, top, right, bottom;
    }

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    public static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool IsWindow(IntPtr hWnd);
    
    [DllImport("user32.dll")]
    static extern int GetDpiForWindow(IntPtr hwnd);
    
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);
    
    const int GWL_EXSTYLE = -20;
    const int WS_EX_TRANSPARENT = 0x20;
    const int WS_EX_LAYERED = 0x80000;

    [DllImport("user32.dll")]
    static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    private const uint GW_HWNDPREV = 3;
    private const uint GW_HWNDNEXT = 2;

    private static readonly IntPtr HWND_BOTTOM      = new(1);
    private static readonly IntPtr HWND_NOTOPMOST   = new(-2);
    private static readonly IntPtr HWND_TOP         = new(0);
    private static readonly IntPtr HWND_TOPMOST     = new(-1);

    private const uint SWP_NOSIZE            = 0x0001;
    private const uint SWP_NOMOVE            = 0x0002;
    private const uint SWP_NOZORDER          = 0x0004;
    private const uint SWP_NOREDRAW          = 0x0008;
    private const uint SWP_NOACTIVATE        = 0x0010;
    private const uint SWP_FRAMECHANGED      = 0x0020;
    private const uint SWP_SHOWWINDOW        = 0x0040;
    private const uint SWP_HIDEWINDOW        = 0x0080;
    private const uint SWP_NOCOPYBITS        = 0x0100;
    private const uint SWP_NOOWNERZORDER     = 0x0200;
    private const uint SWP_NOSENDCHANGING    = 0x0400;

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X,
        int Y,
        int cx,
        int cy,
        uint uFlags);
    
    [DllImport("dwmapi.dll")]
    static extern int DwmGetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        out RECT pvAttribute,
        int cbAttribute
    );

    const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
    
    [StructLayout(LayoutKind.Sequential)]
    struct POINT
    {
        public int x;
        public int y;
    }

    [DllImport("user32.dll")]
    static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);
    
    public const int WM_SETICON = 0x0080;
    public const int ICON_SMALL = 0;
    public const int ICON_BIG = 1;

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr LoadImage(IntPtr hInst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

    public const uint IMAGE_ICON = 1;
    public const uint LR_LOADFROMFILE = 0x00000010;
    public const uint LR_DEFAULTSIZE = 0x00000040;
}