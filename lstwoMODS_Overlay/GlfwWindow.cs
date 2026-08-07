using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Hexa.NET.GLFW;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.GLFW;
using HexaGen.Runtime;
using lstwoMODS.ImGui.Shared;
using lstwoMODS_Overlay.Backends;
using GLFWwindowPtr = Hexa.NET.GLFW.GLFWwindowPtr;

using static lstwoMODS_Overlay.Logger;

namespace lstwoMODS_Overlay;

public abstract class GlfwWindow : Window
{
    protected NativeCallback<GLFWerrorfun> ErrorCallback;

    protected WindowType Type;

    public GLFWwindowPtr GlfwWindowPtr;
    protected IRenderBackend Backend;
    protected IntPtr TargetHwnd = IntPtr.Zero;

    protected (float, float, float, float) ClearColor;

    /// <summary>
    /// True when the current frame has UI that requires keyboard/mouse focus (e.g. an open
    /// chat input). While set, <see cref="TrackTargetWindow"/> continuously keeps the overlay in
    /// the OS foreground (re-grabbing it if the game steals it back) and never pushes focus to
    /// the game. Set each frame by the render loop.
    /// </summary>
    protected volatile bool OverlayWantsInput;

    /// <summary>
    /// One-shot request, set by QueueFocusOverlayWindow, that lets the overlay start grabbing the
    /// foreground immediately (e.g. chat opened via hotkey) before the element's RequireInput
    /// state has propagated over IPC and raised <see cref="OverlayWantsInput"/>.
    /// <see cref="TrackTargetWindow"/> retires it once <see cref="OverlayWantsInput"/> has taken
    /// over so it can't linger and fight a later game-focus request.
    /// </summary>
    protected volatile bool FocusSelfRequested;

    private bool _lastTopMostState;
    private bool _lastHoverState;
    private long _lastForeground;
    private int _lastFbWidth, _lastFbHeight;

    private string _windowTitle;
    private string _iconPath;

    public bool AllowClose { get; set; }

    public GlfwWindow((float, float, float, float) clearColor, WindowType type, string windowTitle, int width, int height, string iconPath = "", bool allowClose = false, IRenderBackend? backend = null)
    {
        ClearColor = clearColor;
        Type = type;
        _windowTitle = windowTitle;
        _iconPath = iconPath;
        AllowClose = allowClose;
        Backend = backend ?? new OpenGL3Backend();
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
        Backend.ConfigureGlfwHints();

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

        if (Backend.IsOpenGL)
        {
            GLFW.MakeContextCurrent(GlfwWindowPtr);
            GLFW.SwapInterval(1);
        }

        if (!string.IsNullOrEmpty(_iconPath))
        {
            GLFW.ShowWindow(GlfwWindowPtr);
            SetIcon(GlfwWindowPtr, _iconPath);
        }

        return true;
    }

    protected override bool CreateGraphicsContext()
    {
        Backend.Initialize(GlfwWindowPtr);
        return true;
    }

    protected override void PollEvents()
    {
        TrackTargetWindow();
        GLFW.PollEvents();
    }

    protected override bool ShouldClose()
    {
        if (GLFW.WindowShouldClose(GlfwWindowPtr) == 0)
            return false;

        if (!AllowClose)
        {
            GLFW.SetWindowShouldClose(GlfwWindowPtr, 0);
            return false;
        }

        return true;
    }

    protected override bool IsMinimized()
    {
        return GLFW.GetWindowAttrib(GlfwWindowPtr, GLFW.GLFW_ICONIFIED) != 0;
    }

    protected override void BeginFrame()
    {
        unsafe
        {
            int fbW, fbH;
            GLFW.GetFramebufferSize(GlfwWindowPtr, &fbW, &fbH);
            if (fbW != _lastFbWidth || fbH != _lastFbHeight)
            {
                _lastFbWidth = fbW;
                _lastFbHeight = fbH;
                Backend.OnResize(fbW, fbH);
            }
        }
        Backend.BeginFrame(Type == WindowType.Overlay, ClearColor.Item1, ClearColor.Item2, ClearColor.Item3, ClearColor.Item4);
    }

    protected override void EndFrame()
    {
        Backend.EndFrame();
    }

    protected override void DestroyGraphicsContext()
    {
        Backend.Shutdown();
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

        var physicalWidth  = rect.right - rect.left;
        var physicalHeight = rect.bottom - rect.top;

        var monitor = MonitorFromWindow(TargetHwnd, MONITOR_DEFAULTTONEAREST);
        if (monitor != IntPtr.Zero)
        {
            var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (GetMonitorInfo(monitor, ref mi) && rect.bottom > mi.rcWork.bottom)
                physicalHeight = mi.rcWork.bottom - rect.top;
        }

        GLFW.SetWindowPos(GlfwWindowPtr, rect.left, rect.top);
        GLFW.SetWindowSize(GlfwWindowPtr, physicalWidth, physicalHeight - 1);

        var hwnd = GLFW.GetWin32Window(GlfwWindowPtr);
        var flags = SWP_NOMOVE | SWP_NOSIZE | SWP_NOREDRAW | SWP_NOACTIVATE;

        if (GetForegroundWindow() == hwnd)
        {
            if (!HasVisibleContent() && !OverlayWantsInput && !FocusSelfRequested)
            {
                SetForegroundWindow(TargetHwnd);
                SetWindowPos(hwnd, GetWindow(TargetHwnd, GW_HWNDNEXT), 0, 0, 0, 0, flags);
                return;
            }

            FocusSelfRequested = false;
            if (GetWindow(hwnd, GW_HWNDNEXT) != TargetHwnd)
                SetWindowPos(TargetHwnd, hwnd, 0, 0, 0, 0, flags);
            return;
        }

        if (HasVisibleContent())
        {
            ClassifyForeground(hwnd, out var fgIsOurs, out var fgIsGame);

            if (fgIsOurs || fgIsGame)
            {
                if (OverlayWantsInput || FocusSelfRequested)
                {
                    if (fgIsGame)
                        FocusOverlayWindow();
                    else if (OverlayWantsInput)
                        FocusSelfRequested = false;
                }
                else
                {
                    FocusSelfRequested = false;
                    if (fgIsOurs)
                        SetForegroundWindow(TargetHwnd);
                }
            }

            SetWindowPos(hwnd, GetWindow(TargetHwnd, GW_HWNDPREV), 0, 0, 0, 0, flags);
        }
        else
        {
            SetWindowPos(hwnd, GetWindow(TargetHwnd, GW_HWNDNEXT), 0, 0, 0, 0, flags);
        }
    }

    /// <summary>
    /// Classify the current foreground window relative to the overlay's context.
    /// <paramref name="fgIsOurs"/> is true when it belongs to the overlay's own process (the main
    /// window or any ImGui multi-viewport platform window); <paramref name="fgIsGame"/> is true
    /// when it is the tracked game window or any other window of the game's process. Both are false
    /// when an unrelated application is in the foreground, which is the signal to stay passive.
    /// </summary>
    private void ClassifyForeground(IntPtr overlayHwnd, out bool fgIsOurs, out bool fgIsGame)
    {
        fgIsOurs = false;
        fgIsGame = false;

        var foreground = GetForegroundWindow();
        if (foreground == IntPtr.Zero) return;

        if (foreground == TargetHwnd) { fgIsGame = true; return; }

        GetWindowThreadProcessId(foreground,  out var fgPid);
        if (fgPid == 0) return;

        GetWindowThreadProcessId(overlayHwnd, out var overlayPid);
        GetWindowThreadProcessId(TargetHwnd,  out var gamePid);

        if (fgPid == overlayPid) fgIsOurs = true;
        else if (fgPid == gamePid) fgIsGame = true;
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

    /// <summary>
    /// Toggle mouse/keyboard pass-through for overlay windows.
    /// When true the overlay is purely visual: mouse clicks fall through to the game and
    /// the window never steals activation. When false it behaves like a normal window.
    /// </summary>
    protected void SetInputPassthrough(bool passthrough)
    {
        var hwnd    = GLFW.GetWin32Window(GlfwWindowPtr);
        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

        SetWindowLong(hwnd, GWL_EXSTYLE, passthrough
            ? exStyle |  WS_EX_NOACTIVATE
            : exStyle & ~WS_EX_NOACTIVATE);

        GLFW.SetWindowAttrib(GlfwWindowPtr, GLFW.GLFW_MOUSE_PASSTHROUGH,
            passthrough ? GLFW.GLFW_TRUE : GLFW.GLFW_FALSE);
    }
    
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
    static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    public void FocusGameWindow()
    {
        if (TargetHwnd != IntPtr.Zero && IsWindow(TargetHwnd))
            SetForegroundWindow(TargetHwnd);
    }

    /// <summary>
    /// Bring the overlay's own OS window to the foreground and give it keyboard focus.
    /// Windows blocks SetForegroundWindow from a process that doesn't own the current
    /// foreground window, so we briefly AttachThreadInput to the foreground thread to lift
    /// that restriction. We also clear WS_EX_NOACTIVATE first (the passthrough flag) so the
    /// window can actually be activated.
    /// </summary>
    public void FocusOverlayWindow()
    {
        var hwnd = GLFW.GetWin32Window(GlfwWindowPtr);
        if (hwnd == IntPtr.Zero) return;

        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle & ~WS_EX_NOACTIVATE);
        GLFW.SetWindowAttrib(GlfwWindowPtr, GLFW.GLFW_MOUSE_PASSTHROUGH, GLFW.GLFW_FALSE);

        var foreground = GetForegroundWindow();
        var thisThread = GetCurrentThreadId();
        var fgThread   = foreground != IntPtr.Zero
            ? GetWindowThreadProcessId(foreground, out _)
            : 0u;

        if (fgThread != 0 && fgThread != thisThread)
        {
            AttachThreadInput(fgThread, thisThread, true);
            BringWindowToTop(hwnd);
            SetForegroundWindow(hwnd);
            AttachThreadInput(fgThread, thisThread, false);
        }
        else
        {
            BringWindowToTop(hwnd);
            SetForegroundWindow(hwnd);
        }

        GLFW.FocusWindow(GlfwWindowPtr);
    }

    [DllImport("user32.dll")]
    static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    static extern bool BringWindowToTop(IntPtr hWnd);
    
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);
    
    const int GWL_EXSTYLE = -20;
    const int WS_EX_TRANSPARENT = 0x20;
    const int WS_EX_LAYERED = 0x80000;
    const int WS_EX_NOACTIVATE = 0x08000000;

    [DllImport("user32.dll")]
    static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

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
    struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [DllImport("user32.dll")]
    static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    const uint MONITOR_DEFAULTTONEAREST = 2;

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