using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Hexa.NET.GLFW;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.GLFW;
using HexaGen.Runtime;
using Microsoft.Win32;
using lstwoMODS.ImGui.Shared;
using lstwoMODS_Overlay.Backends;
using GLFWwindowPtr = Hexa.NET.GLFW.GLFWwindowPtr;

using static lstwoMODS_Overlay.Logger;

namespace lstwoMODS_Overlay;

/// <summary>How a decorated window's OS title bar is themed.</summary>
public enum TitleBarTheme
{
    /// <summary>
    /// Follow the system's app theme, so the caption is the standard Windows one and looks
    /// identical to Explorer, Settings and every other dark-mode-aware app - accent color and
    /// inactive-window shading included. Tracks the setting live.
    /// </summary>
    System,

    /// <summary>
    /// Paint the caption from the window's own background color so it blends into the ImGui theme.
    /// Overrides the system caption entirely (no accent color, no inactive dimming) and needs
    /// Windows 11; on older builds this degrades to the plain dark or light system frame.
    /// </summary>
    MatchImGuiTheme,
}

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
    /// over so it can't linger and fight a later game-focus request, and
    /// <see cref="AgeFocusSelfRequest"/> expires it if none of those paths ever runs.
    /// </summary>
    protected volatile bool FocusSelfRequested;

    private int _lastFbWidth, _lastFbHeight;

    // Last geometry pushed to the overlay window, so the blur-behind is only re-asserted when the
    // window actually moved or resized rather than every frame.
    private bool _hasTrackedRect;
    private int _trackedLeft, _trackedTop, _trackedWidth, _trackedHeight;

    // Desired input pass-through state: -1 = undecided, 0 = interactive, 1 = pass-through.
    // SetInputPassthrough writes it (and skips the transition work when unchanged);
    // EnforceInputPassthrough and SyncMainViewportInputFlag read it every frame.
    private int _passthroughState = -1;

    private string _windowTitle;
    private string _iconPath;

    public bool AllowClose { get; set; }

    /// <summary>
    /// How a decorated window's OS title bar is themed. No effect on overlay windows, which are
    /// undecorated. See <see cref="TitleBarTheme"/>.
    /// </summary>
    public TitleBarTheme TitleBarMode { get; set; } = TitleBarTheme.System;

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

    /// <summary>
    /// Win32 handle of this window, or zero before it has been created. Mainly for owning native
    /// dialogs: an owned window is always above its owner in the z-order, which is what keeps a
    /// dialog in front of an overlay that re-asserts topmost every frame.
    /// </summary>
    public IntPtr NativeHandle
    {
        get
        {
            unsafe
            {
                return GlfwWindowPtr.Handle == null ? IntPtr.Zero : GLFW.GetWin32Window(GlfwWindowPtr);
            }
        }
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

            // Start hidden so the dark caption is in place before the frame is ever composed;
            // a window shown first would flash the default light title bar for a frame or two.
            GLFW.WindowHint(GLFW.GLFW_VISIBLE, GLFW.GLFW_FALSE);
        }

        MainScale = GetMainScale();
        GlfwWindowPtr = GLFW.CreateWindow((int)(1280 * MainScale), (int)(800 * MainScale), _windowTitle, null, null);

        if (GlfwWindowPtr.IsNull)
        {
            LogError("Failed to create GLFW window.");
            GLFW.Terminate();
            return false;
        }

        // Hints are sticky per process, and ImGui's multi-viewport backend creates its platform
        // windows through the same GLFW instance - don't leave GLFW_VISIBLE off behind us.
        GLFW.WindowHint(GLFW.GLFW_VISIBLE, GLFW.GLFW_TRUE);

        if (Backend.IsOpenGL)
        {
            GLFW.MakeContextCurrent(GlfwWindowPtr);
            GLFW.SwapInterval(1);
        }

        ApplyTitleBarTheme();

        if (!string.IsNullOrEmpty(_iconPath))
        {
            GLFW.ShowWindow(GlfwWindowPtr);
            SetIcon(GlfwWindowPtr, _iconPath);
        }
        else if (Type != WindowType.Overlay)
        {
            // Undo the GLFW_VISIBLE hint above for the no-icon case.
            GLFW.ShowWindow(GlfwWindowPtr);
        }

        ApplyFramebufferTransparency();

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
        RefreshSystemTitleBarTheme();
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

        AgeFocusSelfRequest();

        if (TargetHwnd == IntPtr.Zero || !IsWindow(TargetHwnd))
            return;

        if (IsIconic(TargetHwnd))
            return;

        if (!GetClientBounds(TargetHwnd, out var rect))
            return;

        var physicalWidth  = rect.right - rect.left;
        var physicalHeight = rect.bottom - rect.top;

        var clampedToWorkArea = false;
        var monitor = MonitorFromWindow(TargetHwnd, MONITOR_DEFAULTTONEAREST);
        if (monitor != IntPtr.Zero)
        {
            var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (GetMonitorInfo(monitor, ref mi) && rect.bottom > mi.rcWork.bottom)
            {
                physicalHeight    = mi.rcWork.bottom - rect.top;
                clampedToWorkArea = true;
            }
        }

        // Keep the window out of the driver's fullscreen presentation path. An undecorated window
        // whose rect matches the monitor gets presented by flipping instead of through the DWM
        // redirection surface, which drops per-pixel alpha and leaves the overlay opaque black
        // (the overlay always clears to 0,0,0,0).
        //
        // The work-area clamp above only fires for a taskbar docked at the bottom of the game's
        // monitor, so it is a no-op for an auto-hidden taskbar, a taskbar on another monitor, or
        // one docked left/right. In those layouts the window would cover the monitor exactly, and
        // merely shrinking it by a pixel is not reliably enough. Overhang the bottom edge instead:
        // a window that extends past the monitor can never be mistaken for a fullscreen surface.
        // The extra row lands off-monitor (or a pixel into the neighbour below) and the window is
        // transparent and click-through, so it is invisible either way.
        var width  = physicalWidth;
        var height = Math.Max(1, clampedToWorkArea ? physicalHeight - 1 : physicalHeight + 1);

        GLFW.SetWindowPos(GlfwWindowPtr, rect.left, rect.top);
        GLFW.SetWindowSize(GlfwWindowPtr, width, height);

        if (!_hasTrackedRect || rect.left != _trackedLeft || rect.top != _trackedTop ||
            width != _trackedWidth || height != _trackedHeight)
        {
            _hasTrackedRect = true;
            _trackedLeft    = rect.left;
            _trackedTop     = rect.top;
            _trackedWidth   = width;
            _trackedHeight  = height;
            ApplyFramebufferTransparency();
        }

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
            else
            {
                // An unrelated application owns the foreground, so we stay passive - and a queued
                // self-focus is stale by definition here: the user is not in the game, and grabbing
                // the foreground out from under whatever they are actually using would be wrong.
                // Retire it rather than let it fire once they come back.
                FocusSelfRequested = false;
            }

            SetWindowPos(hwnd, GetWindow(TargetHwnd, GW_HWNDPREV), 0, 0, 0, 0, flags);
        }
        else
        {
            SetWindowPos(hwnd, GetWindow(TargetHwnd, GW_HWNDNEXT), 0, 0, 0, 0, flags);
        }
    }

    /// <summary>
    /// Frames a pending <see cref="FocusSelfRequested"/> may wait before it is dropped. It only has
    /// to bridge the IPC round-trip between the mod asking for focus and the element's RequireInput
    /// state arriving, so this is generous; the point is only that it is bounded.
    /// </summary>
    private const int FocusSelfRequestMaxFrames = 30;

    private int _focusSelfRequestFrames;

    /// <summary>
    /// Expire a stale focus-self request.
    ///
    /// The request is a short bridge until <see cref="OverlayWantsInput"/> takes over, not a
    /// standing order. Several paths in <see cref="TrackTargetWindow"/> deliberately act on neither
    /// - nothing is drawn yet, or an unrelated application owns the foreground - and none of them
    /// retire it, so without an expiry a request made at one of those moments stays pending
    /// indefinitely and then fires on some much later frame, pulling focus off the game the instant
    /// the game regains it.
    ///
    /// The counter is only touched here, on the render thread: it resets itself whenever the flag
    /// is clear, so the IPC thread setting the flag needs no synchronisation beyond the volatile.
    /// </summary>
    private void AgeFocusSelfRequest()
    {
        if (!FocusSelfRequested)
        {
            _focusSelfRequestFrames = 0;
            return;
        }

        if (++_focusSelfRequestFrames <= FocusSelfRequestMaxFrames)
            return;

        FocusSelfRequested      = false;
        _focusSelfRequestFrames = 0;
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

    protected abstract bool HasVisibleContent();

    /// <summary>
    /// Toggle mouse/keyboard pass-through for overlay windows.
    /// When true the overlay is purely visual: mouse clicks fall through to the game and
    /// the window never steals activation. When false it behaves like a normal window.
    ///
    /// The bits are applied directly rather than through GLFW's GLFW_MOUSE_PASSTHROUGH attribute,
    /// so that ImGui resetting that attribute every frame cannot silently undo them
    /// (see <see cref="EnforceInputPassthrough"/>).
    ///
    /// Only the transition is handled here; the per-frame re-assert keeps the state alive.
    /// </summary>
    protected void SetInputPassthrough(bool passthrough)
    {
        var desired = passthrough ? 1 : 0;
        if (_passthroughState == desired)
            return;
        _passthroughState = desired;

        if (passthrough) ApplyPassthroughStyles();
        else             ClearPassthroughStyles();
    }

    /// <summary>
    /// Re-assert the pass-through ex-styles, once per frame.
    ///
    /// ImGui's GLFW backend calls glfwSetWindowAttrib(GLFW_MOUSE_PASSTHROUGH, ...) for every
    /// viewport on every ImGuiImplGLFW.NewFrame(), including the main viewport - which never
    /// carries ImGuiViewportFlags_NoInputs, so the call always resolves to "off". GLFW's Win32
    /// implementation of that clears WS_EX_TRANSPARENT, so the overlay silently goes opaque to
    /// hit-testing one frame after <see cref="SetInputPassthrough"/> ran, while still being
    /// WS_EX_NOACTIVATE: clicks meant for the game land on the overlay, activate nothing, and are
    /// lost. Because <see cref="SetInputPassthrough"/> only fires on transitions, nothing ever puts
    /// the bit back.
    ///
    /// Must therefore be called immediately after ImGuiImplGLFW.NewFrame(), every frame. Normally
    /// costs one GetWindowLong plus one SetWindowLong to restore WS_EX_TRANSPARENT; the layered
    /// setup below is a one-off.
    /// </summary>
    protected void EnforceInputPassthrough()
    {
        if (Type != WindowType.Overlay || _passthroughState < 0)
            return;

        var repaired = _passthroughState == 1
            ? ApplyPassthroughStyles()
            : ClearPassthroughStyles();

        if (!repaired)
        {
            _passthroughRepairFrames = 0;
            return;
        }

        // A handful of repairs is normal: SyncMainViewportInputFlag runs before OnRender updates
        // _passthroughState, so the backend acts on a stale flag for a couple of frames after every
        // transition. A sustained run means the flag has stopped steering the backend at all - the
        // likeliest cause being an ImGui or GLFW update that changed how GLFW_MOUSE_PASSTHROUGH is
        // driven. That failure is otherwise completely silent: the styles are still correct by the
        // time anything reads them here, and the only symptom is clicks over the game vanishing
        // whenever one lands in the window between the backend clearing the bit and this restoring
        // it. Say so once rather than let it be rediscovered from scratch.
        if (_passthroughRepairFrames < PassthroughRepairWarnFrames &&
            ++_passthroughRepairFrames == PassthroughRepairWarnFrames)
        {
            LogError(
                $"Input pass-through has needed repairing for {PassthroughRepairWarnFrames} " +
                "consecutive frames. SyncMainViewportInputFlag is no longer stopping ImGui's GLFW " +
                "backend from clearing WS_EX_TRANSPARENT, so clicks over the game will be lost " +
                "intermittently. Check ImGuiViewportFlags_NoInputs handling in the current " +
                "Hexa.NET.ImGui.Backends.GLFW / Hexa.NET.GLFW versions.");
        }
    }

    /// <summary>
    /// Consecutive repaired frames before <see cref="EnforceInputPassthrough"/> reports that it has
    /// gone from backstop to load-bearing. One second at 60fps - far longer than the two or three
    /// frames a legitimate transition costs.
    /// </summary>
    private const int PassthroughRepairWarnFrames = 60;

    private int _passthroughRepairFrames;

    // WS_EX_TRANSPARENT on its own is not enough to take a top-level window out of hit-testing -
    // measured, not assumed: with only TRANSPARENT|NOACTIVATE set, WindowFromPoint still resolved
    // to the overlay. WS_EX_LAYERED is what actually makes the OS route the click past it.
    private const int PassthroughBits = WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_NOACTIVATE;

    /// <summary>Returns true when the styles were not already correct and had to be written.</summary>
    private bool ApplyPassthroughStyles()
    {
        var hwnd = GLFW.GetWin32Window(GlfwWindowPtr);
        if (hwnd == IntPtr.Zero)
            return false;

        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        if ((exStyle & PassthroughBits) == PassthroughBits)
            return false;

        var alreadyLayered = (exStyle & WS_EX_LAYERED) != 0;
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | PassthroughBits);

        // Only the layered setup below is expensive, and it is only needed when the window was not
        // already layered - i.e. once per transition into pass-through, not per frame. ImGui's
        // per-frame reset strips WS_EX_TRANSPARENT but leaves WS_EX_LAYERED alone (see below), so
        // in steady state this method costs the GetWindowLong plus at most the SetWindowLong above.
        if (alreadyLayered)
            return true;

        // A layered window normally draws from its layered attributes, which would replace the DWM
        // blur-behind region this overlay's per-pixel alpha depends on. Declare a fully opaque
        // constant alpha so the layer itself contributes nothing, then re-assert the blur region.
        //
        // Declaring LWA_ALPHA also stops GLFW tearing WS_EX_LAYERED back off: its pass-through
        // disable path only drops that bit when LWA_ALPHA is absent, so ImGui's per-frame reset is
        // left with nothing to strip but WS_EX_TRANSPARENT.
        SetLayeredWindowAttributes(hwnd, 0, 255, LWA_ALPHA);
        ApplyFramebufferTransparency();
        return true;
    }

    /// <summary>Returns true when the styles were not already correct and had to be written.</summary>
    private bool ClearPassthroughStyles()
    {
        var hwnd = GLFW.GetWin32Window(GlfwWindowPtr);
        if (hwnd == IntPtr.Zero)
            return false;

        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        if ((exStyle & PassthroughBits) == 0)
            return false;

        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle & ~PassthroughBits);

        // Dropping WS_EX_LAYERED makes DWM re-evaluate how the window is presented, so the alpha
        // has to be re-asserted or the overlay can end up presenting opaque.
        ApplyFramebufferTransparency();
        return true;
    }

    private const uint LWA_ALPHA = 0x2;

    [DllImport("user32.dll")]
    static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

    /// <summary>
    /// Mirror the pass-through state onto the main viewport's NoInputs flag.
    ///
    /// ImGui's GLFW backend derives its per-frame glfwSetWindowAttrib(GLFW_MOUSE_PASSTHROUGH, ...)
    /// call from this flag. Left alone the main viewport never carries it, so that call always
    /// resolves to "off" and tears WS_EX_TRANSPARENT back off - and however promptly
    /// <see cref="EnforceInputPassthrough"/> restores it, hit-testing runs on the OS input thread
    /// and does not wait for our frame, so a click landing in the gap is still swallowed. (The gap
    /// is not instantaneous: the rest of the backend's NewFrame runs in between, including gamepad
    /// polling.) Setting the flag makes the backend apply the state we already want, so the bit is
    /// never removed in the first place and there is no gap to lose clicks in.
    ///
    /// ImGui rewrites the main viewport's flags inside ImGui.NewFrame(), so this has to be called
    /// after it; the value then stands until the next frame's backend NewFrame reads it.
    /// </summary>
    protected void SyncMainViewportInputFlag()
    {
        if (Type != WindowType.Overlay || _passthroughState < 0)
            return;

        var viewport = Hexa.NET.ImGui.ImGui.GetMainViewport();
        if (viewport.IsNull)
            return;

        if (_passthroughState == 1)
            viewport.Flags |=  Hexa.NET.ImGui.ImGuiViewportFlags.NoInputs;
        else
            viewport.Flags &= ~Hexa.NET.ImGui.ImGuiViewportFlags.NoInputs;
    }

    /// <summary>
    /// Re-assert the DWM blur-behind region that backs the overlay's per-pixel alpha.
    /// GLFW applies this once when the window is created and afterwards only on
    /// WM_DWMCOMPOSITIONCHANGED / WM_DWMCOLORIZATIONCOLORCHANGED, never on a move, a resize or an
    /// ex-style change. Anything that makes DWM re-evaluate the window can therefore leave it
    /// presenting opaque with no path back, so we re-apply it ourselves at those points.
    /// </summary>
    protected void ApplyFramebufferTransparency()
    {
        if (Type != WindowType.Overlay)
            return;

        var hwnd = GLFW.GetWin32Window(GlfwWindowPtr);
        if (hwnd == IntPtr.Zero)
            return;

        // Same shape as GLFW's updateFramebufferTransparency: an empty region means "the whole
        // window uses per-pixel alpha".
        var region = CreateRectRgn(0, 0, -1, -1);
        var bb = new DWM_BLURBEHIND
        {
            dwFlags  = DWM_BB_ENABLE | DWM_BB_BLURREGION,
            fEnable  = 1,
            hRgnBlur = region,
        };

        DwmEnableBlurBehindWindow(hwnd, ref bb);
        DeleteObject(region);
    }

    // ---- OS title bar theming ----------------------------------------------------------------
    //
    // A decorated GLFW window gets the light caption regardless of the system theme, because
    // nothing ever opts the window into the immersive dark frame. That is fine for the overlay
    // (undecorated) but looks broken in compatibility mode, where this is an ordinary desktop
    // window sitting above a dark ImGui theme.
    //
    // DWMWA_USE_IMMERSIVE_DARK_MODE is the OS's own switch for this (Windows 10 1809+): DWM then
    // draws the same caption every other dark-mode app gets, including the accent color when the
    // user has "Show accent color on title bars" on, and the correct active/inactive shading. It
    // is a per-window opt-in with no automatic "follow the system" behaviour, so the app still has
    // to read the user's app-theme preference and flip the flag - that is what TitleBarTheme.System
    // below does, and it is why the caption then matches Explorer and Settings exactly.
    //
    // DWMWA_CAPTION_COLOR/TEXT_COLOR/BORDER_COLOR (Windows 11 22000+) are the escape hatch for
    // painting the caption an arbitrary color instead. That deliberately stops matching the rest of
    // the desktop - it overrides the accent color and the inactive-window dimming - so it is opt-in
    // via TitleBarTheme.MatchImGuiTheme, for when blending into the ImGui theme matters more than
    // looking like every other window.
    //
    // Both fail harmlessly on builds that don't know the attribute, leaving the default frame.

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE          = 20;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_PRE_20H1 = 19;
    private const int DWMWA_BORDER_COLOR                     = 34;
    private const int DWMWA_CAPTION_COLOR                    = 35;
    private const int DWMWA_TEXT_COLOR                       = 36;

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    // Last values pushed to DWM, so the per-frame sync only calls into it when something changed.
    private int  _appliedCaptionColor = -1;
    private bool _appliedDarkMode;
    private bool _hasAppliedTitleBar;

    /// <summary>
    /// Theme the title bar for the first time, at creation and before the window is shown, so the
    /// very first composed frame already has the right caption instead of flashing the default one.
    /// </summary>
    protected void ApplyTitleBarTheme()
    {
        if (Type == WindowType.Overlay)
            return;

        var hwnd = GLFW.GetWin32Window(GlfwWindowPtr);
        if (hwnd == IntPtr.Zero)
            return;

        _hasAppliedTitleBar = true;

        // MatchImGuiTheme has no color to work from until ImGui has been initialised, so it also
        // starts on the system frame and gets repainted by the first SyncTitleBarToClearColor.
        _appliedDarkMode = TitleBarMode == TitleBarTheme.MatchImGuiTheme || IsSystemDarkMode();
        SetImmersiveDarkMode(hwnd, _appliedDarkMode);
    }

    /// <summary>
    /// Re-read the system app theme and flip the caption if the user changed it while we were
    /// running. Windows sends WM_SETTINGCHANGE for this, but GLFW owns the window procedure, so
    /// polling the same preference DWM reads is the cheaper option than subclassing it - a
    /// registry read every couple of seconds, off the frame path's critical work.
    /// </summary>
    private void RefreshSystemTitleBarTheme()
    {
        if (Type == WindowType.Overlay || !_hasAppliedTitleBar ||
            TitleBarMode != TitleBarTheme.System)
            return;

        if (--_systemThemePollFrames > 0)
            return;
        _systemThemePollFrames = SystemThemePollIntervalFrames;

        var dark = IsSystemDarkMode();
        if (dark == _appliedDarkMode)
            return;

        var hwnd = GLFW.GetWin32Window(GlfwWindowPtr);
        if (hwnd == IntPtr.Zero)
            return;

        _appliedDarkMode = dark;
        SetImmersiveDarkMode(hwnd, dark);
    }

    /// <summary>Roughly two seconds at 60fps - a theme switch is a human-scale event.</summary>
    private const int SystemThemePollIntervalFrames = 120;

    private int _systemThemePollFrames = 1;

    /// <summary>
    /// The user's app-theme preference, i.e. the same "Choose your default app mode" setting DWM
    /// and every dark-mode-aware app read. Missing value means light (it is absent on builds
    /// predating the setting).
    /// </summary>
    private static bool IsSystemDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

            return key?.GetValue("AppsUseLightTheme") is int light && light == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Keep the OS title bar in step with the window's clear color, which itself tracks the ImGui
    /// theme (or the user's custom background) - so the caption reads as part of the window rather
    /// than a strip of system chrome bolted on top. Cheap to call every frame: it does nothing
    /// until the color actually changes.
    ///
    /// Only active in <see cref="TitleBarTheme.MatchImGuiTheme"/>, and the colors themselves need
    /// Windows 11; on Windows 10 the window keeps the plain dark/light system frame.
    /// </summary>
    protected void SyncTitleBarToClearColor()
    {
        if (TitleBarMode != TitleBarTheme.MatchImGuiTheme || Type == WindowType.Overlay ||
            !_hasAppliedTitleBar)
            return;

        SetTitleBarColor(new Vector4(ClearColor.Item1, ClearColor.Item2, ClearColor.Item3, 1f));
    }

    /// <summary>
    /// Paint the caption, its text and the window border from a single theme color. The dark-frame
    /// flag is derived from the same color so a light theme gets dark caption text (and a sensibly
    /// light frame on Windows 10, where the explicit colors are unavailable).
    /// </summary>
    public void SetTitleBarColor(Vector4 color)
    {
        if (Type == WindowType.Overlay)
            return;

        var hwnd = GLFW.GetWin32Window(GlfwWindowPtr);
        if (hwnd == IntPtr.Zero)
            return;

        var caption = ToColorRef(color);
        if (caption == _appliedCaptionColor)
            return;
        _appliedCaptionColor = caption;

        // Perceived brightness, so mid-tone themes pick the readable text color rather than
        // flipping on a raw average.
        var dark = 0.2126f * color.X + 0.7152f * color.Y + 0.0722f * color.Z < 0.5f;
        if (dark != _appliedDarkMode)
        {
            SetImmersiveDarkMode(hwnd, dark);
            _appliedDarkMode = dark;
        }

        var text = dark ? 0x00F0F0F0 : 0x00202020;

        DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref caption, sizeof(int));
        DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR,    ref text,    sizeof(int));
        // Same color for the border: a contrasting frame around a caption that already matches the
        // client area just reintroduces the seam this is meant to remove.
        DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR,  ref caption, sizeof(int));
    }

    private static void SetImmersiveDarkMode(IntPtr hwnd, bool enabled)
    {
        var value = enabled ? 1 : 0;

        // 20 is the attribute from Windows 10 20H1 onwards; 19 is the same thing on 1809-19H2,
        // where 20 means something else entirely. Probe rather than version-sniff: the call fails
        // harmlessly on a build that doesn't know the attribute.
        if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int)) != 0)
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_PRE_20H1, ref value, sizeof(int));
    }

    /// <summary>Pack a linear 0..1 color into a Win32 COLORREF (0x00BBGGRR).</summary>
    private static int ToColorRef(Vector4 color)
    {
        static int Channel(float v) => (int)(Math.Max(0f, Math.Min(1f, v)) * 255f + 0.5f);

        return Channel(color.X) | (Channel(color.Y) << 8) | (Channel(color.Z) << 16);
    }

    [StructLayout(LayoutKind.Sequential)]
    struct DWM_BLURBEHIND
    {
        public uint   dwFlags;
        public int    fEnable;
        public IntPtr hRgnBlur;
        public int    fTransitionOnMaximized;
    }

    const uint DWM_BB_ENABLE     = 0x1;
    const uint DWM_BB_BLURREGION = 0x2;

    [DllImport("dwmapi.dll")]
    static extern int DwmEnableBlurBehindWindow(IntPtr hWnd, ref DWM_BLURBEHIND pBlurBehind);

    [DllImport("gdi32.dll")]
    static extern IntPtr CreateRectRgn(int x1, int y1, int x2, int y2);

    [DllImport("gdi32.dll")]
    static extern bool DeleteObject(IntPtr hObject);

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

        // Set the desired state first: ClearPassthroughStyles is also what the per-frame
        // EnforceInputPassthrough calls, and if the state still said "pass-through" it would put
        // the bits straight back on the next frame.
        _passthroughState = 0;
        ClearPassthroughStyles();

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
