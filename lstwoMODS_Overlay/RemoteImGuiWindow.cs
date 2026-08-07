using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Hexa.NET.ImNodes;
using Hexa.NET.ImPlot;
using Hexa.NET.ImPlot3D;
using lstwoMODS_Overlay.UiRenderers;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.Messages;
using lstwoMODS.ImGui.Shared.UI;
using ImGuiDockNodeFlags  = Hexa.NET.ImGui.ImGuiDockNodeFlags;
using ImGuiKey = lstwoMODS.ImGui.Shared.ImGuiKey;
using SharedImGuiConfig   = lstwoMODS.ImGui.Shared.ImGuiConfig;
using SharedImGuiStyleVar = lstwoMODS.ImGui.Shared.ImGuiStyleVar;

namespace lstwoMODS_Overlay;

public class RemoteImGuiWindow : NormalImGuiWindow
{
    public string WindowId;
    public SharedImGuiConfig Config = new();
    public List<FontDescriptor> FontDescriptors = new();

    public List<BaseUIElementData> Elements  = [];
    public Dictionary<int, UIRenderer> Renderers = [];


    private readonly Dictionary<int, BaseUIElementData> _elementById = new();

    private readonly object _elementsLock = new();
    private readonly Queue<FrameStateMessage> _pendingFrameStates = new();
    private volatile bool _awaitingFrameState;
    private volatile bool _focusGameRequested;
    private int           _focusGameWaitFrames;

    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int vKey);

    /// <summary>
    /// Keep ImGui's state for an action key in sync with the real OS key state. Used to defeat a
    /// GLFW key-latch caused by a key-up lost across a focus handoff (see call site): when the
    /// physical key and ImGui disagree, inject the missing edge so a stale latch can't swallow the
    /// next press. A no-op when they already agree, so it never double-fires during normal input.
    /// </summary>
    private void ReconcileActionKey(Hexa.NET.ImGui.ImGuiKey key, int vk)
    {
        var physicallyDown = (GetAsyncKeyState(vk) & 0x8000) != 0;
        if (ImGui.IsKeyDown(key) != physicallyDown)
            ImGui.GetIO().AddKeyEvent(key, physicallyDown);
    }

    [DllImport("user32.dll")] private static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

    [StructLayout(LayoutKind.Sequential)]
    private struct GUITHREADINFO
    {
        public int cbSize;
        public int flags;
        public IntPtr hwndActive, hwndFocus, hwndCapture, hwndMenuOwner, hwndMoveSize, hwndCaret;
        public RECT rcCaret;
    }

    private readonly Dictionary<string, ImFontPtr> _fonts = new();
    private SharedImGuiConfig? _pendingConfig;
    private readonly Queue<FontDescriptor> _pendingFonts = new();
    private readonly Queue<string> _pendingImages = new();
    private volatile string _pendingIniContent = null;


    private readonly List<(string Id, Action Callback)> _frameCallbacks = new();

    private volatile int[] _watchedImGuiKeys = Array.Empty<int>();

    private bool _hadInputCapture;
    private readonly HashSet<int> _hotkeySuppressed = new();

    private bool _lastInputCaptured;

    private readonly Dictionary<int, BaseUIElementData> _pendingRendererData = new();

    private float[] _defaultStyleVarValues  = new float[39];
    private float[] _defaultStyleVarValuesY = new float[39];

    private readonly Dictionary<string, nint> _textureCache = new();
    private readonly Dictionary<string, int> _textureWidths = new();
    private readonly Dictionary<string, int> _textureHeights = new();

    // Passive tracker: the front-most ImGui window this frame (last rendered). Used to
    // remember which tab was active across an F2 hide/show. Overwritten every frame.
    public string LastSelectedWindowTitle;
    // The window to bring to the front at the end of this frame, or null. Holds a captured
    // title (not a live reference to the tracker), so later window renders never clobber it.
    // Fed by the F2 reappear path and by programmatic FocusNextFrame requests.
    public string FocusTargetTitle;

    private readonly List<string> _renderStack = new();

    /// <summary>Apply ImGui ini settings from a string on the next render frame (thread-safe).</summary>
    public void QueueLoadIniSettings(string iniContent)
    {
        _pendingIniContent = iniContent;
    }

    /// <summary>Queue a font to be loaded and the atlas rebuilt on the next render frame.</summary>
    public void QueueFontRegistration(FontDescriptor descriptor)
    {
        lock (_elementsLock) { _pendingFonts.Enqueue(descriptor); }
    }

    /// <summary>Queue an image path to be pre-loaded on the next render frame.</summary>
    public void QueueImagePreload(string filePath)
    {
        lock (_elementsLock) { _pendingImages.Enqueue(filePath); }
    }

    /// <summary>Add a callback that runs on the render thread every ImGui frame, after element rendering.</summary>
    public void AddFrameCallback(string id, Action callback)
    {
        lock (_elementsLock)
        {
            _frameCallbacks.RemoveAll(x => x.Id == id);
            _frameCallbacks.Add((id, callback));
        }
    }

    /// <summary>Remove a previously added frame callback.</summary>
    public void RemoveFrameCallback(string id)
    {
        lock (_elementsLock) { _frameCallbacks.RemoveAll(x => x.Id == id); }
    }

    /// <summary>Update the set of ImGui keys forwarded to the mod side as KeyPressMessages (thread-safe).</summary>
    public void SetWatchedKeys(ImGuiKey[] keys)
    {
        _watchedImGuiKeys = keys != null ? Array.ConvertAll(keys, k => (int)k) : Array.Empty<int>();
    }

    /// <summary>
    /// Load a texture from a file path and return its OpenGL texture ID (as nint).
    /// Returns -1 on failure. Cached, subsequent calls with the same path return the same ID.
    /// Must be called from the render thread (inside a frame callback or renderer's Render()).
    /// </summary>
    public unsafe nint LoadTexture(string path, out int width, out int height)
    {
        if (_textureCache.TryGetValue(path, out var cached))
        {
            width  = _textureWidths.TryGetValue(path, out var w)  ? w : 0;
            height = _textureHeights.TryGetValue(path, out var h) ? h : 0;
            return cached;
        }

        width = height = 0;

        try
        {
            using var bmp = new Bitmap(path);
            int w = bmp.Width, h = bmp.Height;
            var bmpData = bmp.LockBits(
                new Rectangle(0, 0, w, h),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            var pixels = new byte[w * h * 4];
            Marshal.Copy(bmpData.Scan0, pixels, 0, pixels.Length);
            bmp.UnlockBits(bmpData);

            for (var i = 0; i < pixels.Length; i += 4)
            {
                var b = pixels[i + 0];
                var r = pixels[i + 2];
                pixels[i + 0] = r;
                pixels[i + 2] = b;
            }

            var id = Backend.UploadTexture(pixels, w, h);
            _textureCache[path]  = id;
            _textureWidths[path]  = w;
            _textureHeights[path] = h;
            width  = w;
            height = h;
            return id;
        }
        catch (Exception ex)
        {
            Logger.Log($"[RemoteImGuiWindow] Failed to load texture '{path}': {ex.Message}");
            _textureCache[path] = -1;
            return -1;
        }
    }

    // Background (clear color) driven by the plugin config.
    private SharedImGuiConfig.WindowBackgroundMode _backgroundMode = SharedImGuiConfig.WindowBackgroundMode.MatchImGui;
    private float[] _backgroundColor = { 0.45f, 0.55f, 0.60f, 1.0f };

    public RemoteImGuiWindow(string windowId, Action onConfigure, Action onRender, string windowTitle, int width, int height, (float, float, float, float) clearColor = default, WindowType type = WindowType.Normal, string iconPath = "", bool allowClose = false, Backends.IRenderBackend? backend = null)
        : base(windowId, onConfigure, onRender, windowTitle, width, height, clearColor, type, iconPath, allowClose, backend)
    {
        _onRender    += OnRender;
        _onConfigure += OnConfigure;
    }

    /// <summary>
    /// Set the window's clear color from the current background mode. In MatchImGui mode this
    /// reads the live theme's WindowBg, so it is also refreshed each frame. Harmless for overlay
    /// windows, which always clear to transparent regardless of ClearColor.
    /// </summary>
    private void ApplyClearColor()
    {
        switch (_backgroundMode)
        {
            case SharedImGuiConfig.WindowBackgroundMode.Custom:
                var c = _backgroundColor;
                if (c is { Length: >= 4 })
                    ClearColor = (c[0], c[1], c[2], c[3]);
                break;

            default: // MatchImGui
                var bg = ImGui.GetStyle().Colors[(int)Hexa.NET.ImGui.ImGuiCol.WindowBg];
                ClearColor = (bg.X, bg.Y, bg.Z, 1.0f);
                break;
        }
    }

    protected override void ShutdownImGui()
    {
        ImPlot3D.DestroyContext();
        ImNodes.DestroyContext();
        ImPlot.DestroyContext();
        base.ShutdownImGui();
    }

    public virtual void OnConfigure()
    {
        var io = ImGui.GetIO();

        var ctx = ImGui.GetCurrentContext();
        ImPlot.CreateContext();
        ImPlot.SetImGuiContext(ctx);
        ImNodes.CreateContext();
        ImNodes.SetImGuiContext(ctx);
        ImPlot3D.CreateContext();
        ImGuizmo.SetImGuiContext(ctx);

        ReadStyleVars(ImGui.GetStyle(), _defaultStyleVarValues, _defaultStyleVarValuesY);

        ApplyConfigToIO(Config);

        unsafe
        {
            var excludePua = stackalloc uint[] { 0xE000, 0xF8FF, 0 };
            var interCfg = new ImFontConfig
            {
                FontDataOwnedByAtlas = 1,
                GlyphMaxAdvanceX     = float.MaxValue,
                RasterizerMultiply   = 1.0f,
                RasterizerDensity    = 1.0f,
                GlyphExcludeRanges   = excludePua,
            };
            io.Fonts.AddFontFromFileTTF("Assets/InterVariable.ttf", 18, &interCfg);

            if (File.Exists("Assets/lucide.ttf"))
                AddFont(io, new FontDescriptor { FilePath = "Assets/lucide.ttf", Size = 15, Merge = true, GlyphOffsetY = 2.5f });

            foreach (var descriptor in FontDescriptors)
                AddFont(io, descriptor);
        }
    }

    /// <summary>Add a font from a descriptor. Merge descriptors get an ImFontConfig with
    /// MergeMode so their glyphs extend the previously added font; they are not registered
    /// under a name (a merged font is not independently pushable).</summary>
    private unsafe void AddFont(ImGuiIOPtr io, FontDescriptor descriptor)
    {
        if (!descriptor.Merge)
        {
            var ptr = io.Fonts.AddFontFromFileTTF(descriptor.FilePath, descriptor.Size);
            if (descriptor.Name != null) _fonts[descriptor.Name] = ptr;
            return;
        }

        var cfg = new ImFontConfig
        {
            FontDataOwnedByAtlas = 1,
            GlyphMaxAdvanceX     = float.MaxValue,
            RasterizerMultiply   = 1.0f,
            RasterizerDensity    = 1.0f,
            MergeMode            = 1,
            GlyphOffset          = new Vector2(0f, descriptor.GlyphOffsetY),
        };
        io.Fonts.AddFontFromFileTTF(descriptor.FilePath, descriptor.Size, &cfg);
    }

    protected override void RenderFrame()
    {
        var pendingIni = _pendingIniContent;
        if (pendingIni != null)
        {
            _pendingIniContent = null;
            ImGui.LoadIniSettingsFromMemory(pendingIni);
        }
        base.RenderFrame();
    }

    protected override void EndFrame()
    {
        base.EndFrame();

        if (_focusGameRequested)
        {
            if (!OverlayWantsInput || ++_focusGameWaitFrames >= 30)
            {
                _focusGameRequested  = false;
                _focusGameWaitFrames = 0;
                FocusGameWindow();
            }
        }
    }

    /// <summary>
    /// Request the overlay window grab OS foreground + keyboard focus. The actual grab is
    /// performed by TrackTargetWindow once an element is actually requiring input, so the
    /// grab and the "don't steal focus back to the game" guard are driven by the same signal
    /// and can't race each other.
    /// </summary>
    public void QueueFocusOverlayWindow()
    {
        FocusSelfRequested = true;
    }

    /// <summary>
    /// Request a game-window focus. The focus is deferred (see EndFrame) until the overlay has
    /// actually stopped requiring input, i.e. its panels have hidden, so the game window isn't
    /// brought forward while TrackTargetWindow would still fight to keep the overlay focused.
    /// </summary>
    public void QueueFocusGameWindow()
    {
        _focusGameRequested  = true;
        _focusGameWaitFrames = 0;
    }

    public virtual void OnRender()
    {
        // MatchImGui tracks the live theme, so refresh it every frame (custom/default are static).
        if (_backgroundMode == SharedImGuiConfig.WindowBackgroundMode.MatchImGui)
            ApplyClearColor();

        while (true)
        {
            FrameStateMessage state;
            lock (_elementsLock)
            {
                if (_pendingFrameStates.Count == 0) break;
                state = _pendingFrameStates.Dequeue();
            }
            ApplyFrameStateNow(state);
        }

        SharedImGuiConfig? pending;
        lock (_elementsLock)
        {
            pending        = _pendingConfig;
            _pendingConfig = null;
        }
        if (pending != null)
            ApplyConfigToIO(pending);


        FontDescriptor[] pendingFonts;
        lock (_elementsLock)
        {
            pendingFonts = _pendingFonts.Count > 0 ? _pendingFonts.ToArray() : null;
            _pendingFonts.Clear();
        }
        if (pendingFonts != null)
        {
            unsafe
            {
                var io = ImGui.GetIO();
                foreach (var desc in pendingFonts)
                    AddFont(io, desc);
            }

            Backend.RebuildFontTexture();
        }


        string[] pendingImages;
        lock (_elementsLock)
        {
            pendingImages = _pendingImages.Count > 0 ? _pendingImages.ToArray() : null;
            _pendingImages.Clear();
        }
        if (pendingImages != null)
        {
            foreach (var path in pendingImages)
                LoadTexture(path, out _, out _);
        }

        ImGui.DockSpaceOverViewport(Config.DockSpaceOverViewportId, ImGui.GetMainViewport(), ImGuiDockNodeFlags.PassthruCentralNode);
        ImGuizmo.BeginFrame();

        List<BaseUIElementData> snapshot;
        lock (_elementsLock)
        {
            snapshot = [..Elements];
        }

        var anyRequiresInput = false;
        foreach (var element in snapshot.Where(e => e.Enabled))
        {
            RenderSingleElement(element);
            if (!anyRequiresInput && ElementTreeRequiresInput(element))
                anyRequiresInput = true;
        }

        if (Type == WindowType.Overlay)
        {
            SetInputPassthrough(!anyRequiresInput);
            OverlayWantsInput = anyRequiresInput;

            if (anyRequiresInput != _lastInputCaptured)
            {
                _lastInputCaptured = anyRequiresInput;
                var io = ImGui.GetIO();
                io.ClearInputKeys();
                io.ClearInputMouse();
            }

            if (anyRequiresInput && GetForegroundWindow() == Hexa.NET.GLFW.GLFW.GetWin32Window(GlfwWindowPtr))
            {
                ReconcileActionKey(Hexa.NET.ImGui.ImGuiKey.Enter,  0x0D); // VK_RETURN (also set for keypad Enter)
                ReconcileActionKey(Hexa.NET.ImGui.ImGuiKey.Escape, 0x1B); // VK_ESCAPE
            }
        }

        List<(string Id, Action Callback)> callbacks;
        lock (_elementsLock) { callbacks = new List<(string, Action)>(_frameCallbacks); }
        foreach (var (_, cb) in callbacks)
        {
            try { cb(); }
            catch (Exception ex) { Logger.Log($"[FrameCallback] Exception: {ex.Message}"); }
        }

        PollHotkeys(Type != WindowType.Overlay || anyRequiresInput);

        if (FocusTargetTitle != null)
        {
            ImGui.SetWindowFocus(FocusTargetTitle);
            FocusTargetTitle = null;
        }

        if (!_awaitingFrameState)
        {
            _awaitingFrameState = true;
            Program.IpcChannel.SendMessage(new FrameRequestMessage
            {
                WindowId       = WindowId,
                OutputElements = CollectOutput()
            }.Serialize());
        }
    }

    private void PollHotkeys(bool inputCaptured)
    {
        var keys = _watchedImGuiKeys;
        if (keys.Length == 0) return;

        if (!inputCaptured)
        {
            _hadInputCapture = false;
            return;
        }

        if (!_hadInputCapture)
        {
            _hadInputCapture = true;
            _hotkeySuppressed.Clear();
            foreach (var key in keys)
                _hotkeySuppressed.Add(key);
        }

        var ctrlHeld  = ImGui.IsKeyDown(Hexa.NET.ImGui.ImGuiKey.LeftCtrl)  || ImGui.IsKeyDown(Hexa.NET.ImGui.ImGuiKey.RightCtrl);
        var shiftHeld = ImGui.IsKeyDown(Hexa.NET.ImGui.ImGuiKey.LeftShift) || ImGui.IsKeyDown(Hexa.NET.ImGui.ImGuiKey.RightShift);
        var altHeld   = ImGui.IsKeyDown(Hexa.NET.ImGui.ImGuiKey.LeftAlt)   || ImGui.IsKeyDown(Hexa.NET.ImGui.ImGuiKey.RightAlt);

        var modifiers = HotkeyModifiers.None;
        if (ctrlHeld)  modifiers |= HotkeyModifiers.Ctrl;
        if (shiftHeld) modifiers |= HotkeyModifiers.Shift;
        if (altHeld)   modifiers |= HotkeyModifiers.Alt;

        foreach (var key in keys)
        {
            if (_hotkeySuppressed.Count > 0 && _hotkeySuppressed.Contains(key))
            {
                if (!ImGui.IsKeyDown((Hexa.NET.ImGui.ImGuiKey)key))
                    _hotkeySuppressed.Remove(key);
                continue;
            }

            if (ImGui.IsKeyPressed((Hexa.NET.ImGui.ImGuiKey)key, false))
                Program.IpcChannel.SendMessage(new KeyPressMessage { WindowId = WindowId, ImGuiKey = key, Modifiers = modifiers }.Serialize());
        }
    }

    /// <summary>Queue a config update to be applied on the next render frame (thread-safe).</summary>
    public void ApplyConfig(SharedImGuiConfig config)
    {
        lock (_elementsLock)
        {
            _pendingConfig = config;
        }
    }

    /// <summary>
    /// Set the initial top-level elements and register every descendant into _elementById.
    /// Call this instead of setting Elements directly so visibility updates work even for
    /// elements that haven't been rendered yet.
    /// </summary>
    public void InitializeElements(IEnumerable<BaseUIElementData> elements)
    {
        lock (_elementsLock)
        {
            Elements = elements.ToList();
            foreach (var data in Elements)
                RegisterElementTree(data);
        }
    }

    /// <summary>
    /// UpdatedElements arrive without their Children (shallow serialization, see
    /// FrameStateMessage.Serialize). When the incoming list is empty, copy the previously
    /// registered child list back onto it so tree walks over Data.Children stay correct
    /// (rendering, and crucially the input-passthrough check in ElementTreeRequiresInput).
    /// </summary>
    private static readonly string[] _childListProperties = ["Children", "LineChildren"];

    private static void RestoreStrippedChildren(BaseUIElementData existing, BaseUIElementData incoming)
    {
        if (existing == null) return;
        foreach (var name in _childListProperties)
        {
            var prop = incoming.GetType().GetProperty(name);
            if (prop != null
                && prop.GetValue(existing) is List<BaseUIElementData> existingChildren
                && existingChildren.Count > 0
                && prop.GetValue(incoming) is List<BaseUIElementData> dataChildren
                && dataChildren.Count == 0)
            {
                prop.SetValue(incoming, existingChildren);
            }
        }
    }

    // Child id → parent id, maintained by RegisterElementTree. Lets RemovedElementIds
    // splice nested elements out of their parent's child list.
    private readonly Dictionary<int, int> _childToParent = new();

    /// <summary>The element's main Children list, the one runtime creates splice into.</summary>
    private static List<BaseUIElementData>? GetChildrenOf(BaseUIElementData data)
        => data.GetType().GetProperty("Children")?.GetValue(data) as List<BaseUIElementData>;

    /// <summary>Every child list of the element (Children, LineChildren, ...) for tree walks.</summary>
    private static IEnumerable<List<BaseUIElementData>> GetChildListsOf(BaseUIElementData data)
    {
        foreach (var name in _childListProperties)
        {
            if (data.GetType().GetProperty(name)?.GetValue(data) is List<BaseUIElementData> list)
                yield return list;
        }
    }

    private void RegisterElementTree(BaseUIElementData data)
    {
        _elementById[data.Id] = data;
        foreach (var children in GetChildListsOf(data))
        foreach (var child in children)
        {
            _childToParent[child.Id] = data.Id;
            RegisterElementTree(child);
        }
    }

    private void UnregisterElementTree(BaseUIElementData data)
    {
        _elementById.Remove(data.Id);
        Renderers.Remove(data.Id);
        _pendingRendererData.Remove(data.Id);
        _childToParent.Remove(data.Id);
        foreach (var children in GetChildListsOf(data))
        foreach (var child in children)
            UnregisterElementTree(child);
    }

    private bool ElementTreeRequiresInput(BaseUIElementData data, bool parentRequires = true)
    {
        var live = Renderers.TryGetValue(data.Id, out var r) ? r.Data
                 : _elementById.TryGetValue(data.Id, out var byId) ? byId
                 : data;

        if (!live.Enabled) return false;

        if (r != null && !r.IsOnMainViewport) return false;

        // A closed/hidden container (window, modal, popup) renders nothing and captures no input,
        // but its subtree can still resolve RequireInput to true (Inherit → the parent's effective
        // value, which is true at the root). Skip the whole subtree so a mounted-but-closed
        // container can't keep OverlayWantsInput set and pin the overlay in the foreground while
        // nothing is on screen.
        if (r != null && !r.ParticipatesInInput) return false;

        var effective = live.RequireInput switch
        {
            RequireInputMode.True  => true,
            RequireInputMode.False => false,
            _                     => parentRequires,
        };

        var prop = live.GetType().GetProperty("Children");
        if (prop?.GetValue(live) is List<BaseUIElementData> { Count: > 0 } children)
        {
            foreach (var child in children)
                if (ElementTreeRequiresInput(child, effective)) return true;
            return false;
        }

        return effective;
    }

    public virtual void RenderSingleElement(BaseUIElementData element)
    {
        var liveData = Renderers.TryGetValue(element.Id, out var liveRenderer)
            ? liveRenderer.Data
            : _pendingRendererData.TryGetValue(element.Id, out var pending) ? pending
            : _elementById.TryGetValue(element.Id, out var byId) ? byId
            : element;
        if (!liveData.Enabled) return;

        var typeName = element.GetType().Name;
        if (typeName.EndsWith("Data")) typeName = typeName[..^4];
        _renderStack.Add($"{typeName}(\"{element.Name ?? "null"}\")");

        try
        {
            var styleVarCount    = 0;
            var styleColorCount  = 0;
            var idCount          = 0;
            var fontPushCount    = 0;
            var itemWidthCount   = 0;
            var itemFlagCount    = 0;
            var disabledCount    = 0;
            var textWrapPosCount = 0;
            var clipRectCount    = 0;

            foreach (var cmd in liveData.PushCommands)
            {
                switch (cmd)
                {
                    case PushFontCommand fontCmd:
                        var fontPtr = fontCmd.FontName != null && _fonts.TryGetValue(fontCmd.FontName, out var f)
                            ? f
                            : default;
                        ImGui.PushFont(fontPtr, 0);
                        fontPushCount++;
                        break;

                    case PushStyleVarCommand varCmd:
                        ImGui.PushStyleVar(
                            (Hexa.NET.ImGui.ImGuiStyleVar)(int)varCmd.Var,
                            varCmd.Value);
                        styleVarCount++;
                        break;

                    case PushStyleVarVec2Command vec2Cmd:
                        ImGui.PushStyleVar(
                            (Hexa.NET.ImGui.ImGuiStyleVar)(int)vec2Cmd.Var,
                            new Vector2(vec2Cmd.X, vec2Cmd.Y));
                        styleVarCount++;
                        break;

                    case PushStyleColorCommand colCmd:
                        ImGui.PushStyleColor(
                            (Hexa.NET.ImGui.ImGuiCol)(int)colCmd.Col,
                            new Vector4(colCmd.R, colCmd.G, colCmd.B, colCmd.A));
                        styleColorCount++;
                        break;

                    case PushStyleColorAlphaCommand colACmd:
                        {
                            var liveCol = ImGui.GetStyle().Colors[(int)colACmd.Col];
                            ImGui.PushStyleColor(
                                (Hexa.NET.ImGui.ImGuiCol)(int)colACmd.Col,
                                new Vector4(liveCol.X, liveCol.Y, liveCol.Z, colACmd.A));
                            styleColorCount++;
                        }
                        break;

                    case PushIdCommand idCmd:
                        ImGui.PushID(idCmd.Id);
                        idCount++;
                        break;

                    case PushItemWidthCommand iwCmd:
                        ImGui.PushItemWidth(iwCmd.Width);
                        itemWidthCount++;
                        break;

                    case PushItemFlagCommand ifCmd:
                        ImGui.PushItemFlag(
                            (Hexa.NET.ImGui.ImGuiItemFlags)(int)ifCmd.Flags,
                            ifCmd.Enable);
                        itemFlagCount++;
                        break;

                    case PushDisabledCommand disCmd:
                        ImGui.BeginDisabled(disCmd.Disabled);
                        disabledCount++;
                        break;

                    case PushTextWrapPosCommand twpCmd:
                        ImGui.PushTextWrapPos(twpCmd.WrapPosX);
                        textWrapPosCount++;
                        break;

                    case PushClipRectCommand crCmd:
                        ImGui.PushClipRect(
                            new Vector2(crCmd.MinX, crCmd.MinY),
                            new Vector2(crCmd.MaxX, crCmd.MaxY),
                            crCmd.IntersectWithCurrent);
                        clipRectCount++;
                        break;

                    case PushTabStopCommand:
                        break;
                }
            }


            try
            {
                if (!Renderers.TryGetValue(element.Id, out var renderer))
                {
                    _pendingRendererData.TryGetValue(element.Id, out var initData);
                    _pendingRendererData.Remove(element.Id);
                    var rendererType = UIRendererRegistry.DataToRenderer[element.GetType()];
                    renderer         = (UIRenderer)Activator.CreateInstance(rendererType, initData ?? element)!;
                    renderer.Window  = this;
                    Renderers[element.Id] = renderer;
                }
                var showChildren = renderer.RenderWidget();


                if (!string.IsNullOrEmpty(liveData.Tooltip))
                {
                    if (liveData.TooltipHoveredFlags == 0)
                    {
                        ImGui.SetItemTooltip(liveData.Tooltip);
                    }
                    else
                    {
                        var flags = (Hexa.NET.ImGui.ImGuiHoveredFlags)(int)liveData.TooltipHoveredFlags;
                        if (ImGui.IsItemHovered(flags))
                            ImGui.SetTooltip(liveData.Tooltip);
                    }
                }

                if (showChildren)
                    renderer.RenderChildren();
            }
            catch (Exception ex)
            {
                CrashGuard.Report($"UI element [{GetRenderBreadcrumb()}]", ex);
            }
            finally
            {
                for (var i = 0; i < clipRectCount;    i++) ImGui.PopClipRect();
                for (var i = 0; i < textWrapPosCount; i++) ImGui.PopTextWrapPos();
                for (var i = 0; i < disabledCount;    i++) ImGui.EndDisabled();
                for (var i = 0; i < itemFlagCount;    i++) ImGui.PopItemFlag();
                for (var i = 0; i < itemWidthCount;   i++) ImGui.PopItemWidth();
                for (var i = 0; i < idCount;          i++) ImGui.PopID();
                if (styleColorCount > 0) ImGui.PopStyleColor(styleColorCount);
                if (styleVarCount   > 0) ImGui.PopStyleVar(styleVarCount);
                for (var i = 0; i < fontPushCount;    i++) ImGui.PopFont();
            }
        }
        finally
        {
            _renderStack.RemoveAt(_renderStack.Count - 1);
        }
    }


    private string GetRenderBreadcrumb() => _renderStack.Count > 0
        ? string.Join(" > ", _renderStack)
        : "(empty stack)";

    /// <summary>
    /// Queue a frame state received from the mod side. It is applied on the render thread at
    /// the start of the next frame, never here on the IPC thread: renderers hold their child
    /// lists by reference, so splicing creates/removes in from another thread races the render
    /// loop's enumeration of those same lists ("Collection was modified" mid-frame crashes).
    /// </summary>
    public void ApplyFrameState(FrameStateMessage state)
    {
        lock (_elementsLock)
        {
            _pendingFrameStates.Enqueue(state);
        }
    }

    private void ApplyFrameStateNow(FrameStateMessage state)
    {
        lock (_elementsLock)
        {
            foreach (var entry in state.CreatedElements)
            {
                var data = entry.Data;
                var attached = false;

                if (_elementById.ContainsKey(data.Id))
                {
                    Logger.Log($"Created element {data.Id} is already registered. Skipping duplicate create.");
                    continue;
                }

                if (entry.ParentId >= 0 && _elementById.TryGetValue(entry.ParentId, out var parentData))
                {
                    if (GetChildrenOf(parentData) is { } siblings)
                    {
                        var index = entry.Index < 0 || entry.Index > siblings.Count
                            ? siblings.Count
                            : entry.Index;
                        siblings.Insert(index, data);
                        _childToParent[data.Id] = parentData.Id;
                        attached = true;
                    }
                    else
                    {
                        Logger.Log($"Created element {data.Id} targets parent {entry.ParentId} which has no child list. Adding top-level.");
                    }
                }
                else if (entry.ParentId >= 0)
                {
                    Logger.Log($"Created element {data.Id} targets unknown parent {entry.ParentId}. Adding top-level.");
                }

                if (!attached)
                    Elements.Add(data);
                RegisterElementTree(data);
            }

            foreach (var data in state.UpdatedElements)
            {
                _elementById.TryGetValue(data.Id, out var existing);

                RestoreStrippedChildren(existing, data);

                if (Renderers.TryGetValue(data.Id, out var renderer))
                {
                    renderer.ApplyState(data);
                    RegisterElementTree(data);

                    var idx = Elements.FindIndex(e => e.Id == data.Id);
                    if (idx >= 0)
                    {
                        Elements[idx] = data;
                    }
                    else if (existing != null && !ReferenceEquals(existing, data))
                    {
                        existing.Enabled      = data.Enabled;
                        existing.Name         = data.Name;
                        existing.PushCommands = data.PushCommands;
                        _elementById[data.Id] = existing;
                    }
                }
                else
                {
                    _elementById[data.Id]         = data;
                    _pendingRendererData[data.Id] = data;

                    var idx = Elements.FindIndex(e => e.Id == data.Id);
                    if (idx >= 0)
                        Elements[idx] = data;
                }
            }

            foreach (var id in state.RemovedElementIds)
            {
                var topLevel = Elements.FirstOrDefault(e => e.Id == id);
                if (topLevel != null)
                {
                    Elements.Remove(topLevel);
                }
                else if (_childToParent.TryGetValue(id, out var parentId)
                         && _elementById.TryGetValue(parentId, out var parentData))
                {
                    foreach (var childList in GetChildListsOf(parentData))
                        childList.RemoveAll(c => c.Id == id);
                }

                if (_elementById.TryGetValue(id, out var data))
                    UnregisterElementTree(data); // subtree renderers/registrations too
                else
                    Renderers.Remove(id);
            }
            _awaitingFrameState = false;
        }
    }

    private BaseUIElementData[] CollectOutput()
    {
        var output = new List<BaseUIElementData>();

        lock (_elementsLock)
        {
            foreach (var renderer in Renderers.Values)
            {
                var state = renderer.GetNewState();
                if (state != null)
                    output.Add(state);
            }
        }

        return output.ToArray();
    }

    private unsafe void ApplyConfigToIO(SharedImGuiConfig config)
    {
        var io    = ImGui.GetIO();
        var style = ImGui.GetStyle();

        io.ConfigFlags = (Hexa.NET.ImGui.ImGuiConfigFlags)(int)config.ConfigFlags;

        // Mouse / cursor
        io.MouseDrawCursor           = config.MouseDrawCursor;
        io.MouseCtrlLeftAsRightClick = config.MouseCtrlLeftAsRightClick;
        io.MouseDoubleClickTime      = config.MouseDoubleClickTime;
        io.MouseDoubleClickMaxDist   = config.MouseDoubleClickMaxDist;
        io.MouseDragThreshold        = config.MouseDragThreshold;

        // Keyboard / input
        io.KeyRepeatDelay               = config.KeyRepeatDelay;
        io.KeyRepeatRate                = config.KeyRepeatRate;
        io.ConfigInputTextCursorBlink   = config.ConfigInputTextCursorBlink;
        io.ConfigInputTextEnterKeepActive = config.ConfigInputTextEnterKeepActive;
        io.ConfigDragClickToInputText   = config.ConfigDragClickToInputText;
        io.ConfigInputTrickleEventQueue = config.ConfigInputTrickleEventQueue;
        io.ConfigMacOSXBehaviors        = config.ConfigMacOSXBehaviors;

        // Navigation
        io.ConfigNavSwapGamepadButtons    = config.ConfigNavSwapGamepadButtons;
        io.ConfigNavCaptureKeyboard       = config.ConfigNavCaptureKeyboard;
        io.ConfigNavCursorVisibleAlways   = config.ConfigNavCursorVisibleAlways;
        io.ConfigNavCursorVisibleAuto     = config.ConfigNavCursorVisibleAuto;
        io.ConfigNavEscapeClearFocusItem  = config.ConfigNavEscapeClearFocusItem;
        io.ConfigNavEscapeClearFocusWindow= config.ConfigNavEscapeClearFocusWindow;
        io.ConfigNavMoveSetMousePos       = config.ConfigNavMoveSetMousePos;

        // Windows
        io.ConfigWindowsResizeFromEdges      = config.ConfigWindowsResizeFromEdges;
        io.ConfigWindowsMoveFromTitleBarOnly = config.ConfigWindowsMoveFromTitleBarOnly;
        io.ConfigWindowsCopyContentsWithCtrlC = config.ConfigWindowsCopyContentsWithCtrlC;
        io.ConfigScrollbarScrollByPage       = config.ConfigScrollbarScrollByPage;

        // Docking
        io.ConfigDockingAlwaysTabBar       = config.ConfigDockingAlwaysTabBar;
        io.ConfigDockingNoSplit            = config.ConfigDockingNoSplit;
        io.ConfigDockingTransparentPayload = config.ConfigDockingTransparentPayload;
        io.ConfigDockingWithShift          = config.ConfigDockingWithShift;

        // Viewports
        io.ConfigViewportsNoAutoMerge               = config.ConfigViewportsNoAutoMerge;
        io.ConfigViewportsNoDecoration              = config.ConfigViewportsNoDecoration;
        io.ConfigViewportsNoDefaultParent           = config.ConfigViewportsNoDefaultParent;
        io.ConfigViewportsNoTaskBarIcon             = config.ConfigViewportsNoTaskBarIcon;
        io.ConfigViewportPlatformFocusSetsImGuiFocus= config.ConfigViewportPlatformFocusSetsImGuiFocus;

        // Fonts / DPI
        style.FontScaleMain      = config.FontGlobalScale;
        io.FontAllowUserScaling  = config.FontAllowUserScaling;
        io.ConfigDpiScaleFonts   = config.ConfigDpiScaleFonts;
        io.ConfigDpiScaleViewports = config.ConfigDpiScaleViewports;

        // Memory / ini
        io.ConfigMemoryCompactTimer = config.ConfigMemoryCompactTimer;
        io.IniSavingRate            = config.IniSavingRate;
        if (config.DisableIniSave)
            io.IniFilename = null;

        // Error recovery
        io.ConfigErrorRecovery               = config.ConfigErrorRecovery;
        io.ConfigErrorRecoveryEnableAssert   = config.ConfigErrorRecoveryEnableAssert;
        io.ConfigErrorRecoveryEnableDebugLog = config.ConfigErrorRecoveryEnableDebugLog;
        io.ConfigErrorRecoveryEnableTooltip  = config.ConfigErrorRecoveryEnableTooltip;

        // Debug
        io.ConfigDebugIsDebuggerPresent              = config.ConfigDebugIsDebuggerPresent;
        io.ConfigDebugBeginReturnValueOnce           = config.ConfigDebugBeginReturnValueOnce;
        io.ConfigDebugBeginReturnValueLoop           = config.ConfigDebugBeginReturnValueLoop;
        io.ConfigDebugIgnoreFocusLoss                = config.ConfigDebugIgnoreFocusLoss;
        io.ConfigDebugIniSettings                    = config.ConfigDebugIniSettings;
        io.ConfigDebugHighlightIdConflicts           = config.ConfigDebugHighlightIdConflicts;
        io.ConfigDebugHighlightIdConflictsShowItemPicker = config.ConfigDebugHighlightIdConflictsShowItemPicker;

        ApplyGlobalStyle(config, style);

        // Window background (clear color)  computed after the theme/global style so MatchImGui
        // reads the final WindowBg.
        _backgroundMode  = config.BackgroundMode;
        _backgroundColor = config.WindowBackgroundColor ?? _backgroundColor;
        ApplyClearColor();
    }

    /// <summary>
    /// Respond to a <see cref="RequestStyleDataMessage"/> by applying the requested theme to a
    /// temporary copy of the style, reading colors + default vars, then sending a
    /// <see cref="StyleDataMessage"/> back, all on the render thread.
    /// </summary>
    public void HandleStyleDataRequest(int themeIndex, string requestId)
    {
        AddFrameCallback("style-req-" + requestId, () =>
        {
            var style = ImGui.GetStyle();

            var saved = new System.Numerics.Vector4[60];
            for (var i = 0; i < 60; i++) saved[i] = style.Colors[i];

            switch (themeIndex)
            {
                case 0: ImGui.StyleColorsDark();    break;
                case 1: ImGui.StyleColorsLight();   break;
                case 2: ImGui.StyleColorsClassic(); break;
            }

            var colors = new float[60 * 4];
            for (var i = 0; i < 60; i++)
            {
                var c = style.Colors[i];
                colors[i * 4]     = c.X;
                colors[i * 4 + 1] = c.Y;
                colors[i * 4 + 2] = c.Z;
                colors[i * 4 + 3] = c.W;
            }

            for (var i = 0; i < 60; i++) style.Colors[i] = saved[i];

            Program.IpcChannel.SendMessage(new StyleDataMessage
            {
                RequestId       = requestId,
                Colors          = colors,
                StyleVarValues  = _defaultStyleVarValues,
                StyleVarValuesY = _defaultStyleVarValuesY,
            }.Serialize());

            RemoveFrameCallback("style-req-" + requestId);
        });
    }

    private static void ReadStyleVars(ImGuiStylePtr s, float[] vals, float[] valsY)
    {
        vals[(int)SharedImGuiStyleVar.Alpha]                        = s.Alpha;
        vals[(int)SharedImGuiStyleVar.DisabledAlpha]                = s.DisabledAlpha;
        vals[(int)SharedImGuiStyleVar.WindowPadding]                = s.WindowPadding.X;    valsY[(int)SharedImGuiStyleVar.WindowPadding]                = s.WindowPadding.Y;
        vals[(int)SharedImGuiStyleVar.WindowRounding]               = s.WindowRounding;
        vals[(int)SharedImGuiStyleVar.WindowBorderSize]             = s.WindowBorderSize;
        vals[(int)SharedImGuiStyleVar.WindowMinSize]                = s.WindowMinSize.X;    valsY[(int)SharedImGuiStyleVar.WindowMinSize]                = s.WindowMinSize.Y;
        vals[(int)SharedImGuiStyleVar.WindowTitleAlign]             = s.WindowTitleAlign.X; valsY[(int)SharedImGuiStyleVar.WindowTitleAlign]             = s.WindowTitleAlign.Y;
        vals[(int)SharedImGuiStyleVar.ChildRounding]                = s.ChildRounding;
        vals[(int)SharedImGuiStyleVar.ChildBorderSize]              = s.ChildBorderSize;
        vals[(int)SharedImGuiStyleVar.PopupRounding]                = s.PopupRounding;
        vals[(int)SharedImGuiStyleVar.PopupBorderSize]              = s.PopupBorderSize;
        vals[(int)SharedImGuiStyleVar.FramePadding]                 = s.FramePadding.X;     valsY[(int)SharedImGuiStyleVar.FramePadding]                 = s.FramePadding.Y;
        vals[(int)SharedImGuiStyleVar.FrameRounding]                = s.FrameRounding;
        vals[(int)SharedImGuiStyleVar.FrameBorderSize]              = s.FrameBorderSize;
        vals[(int)SharedImGuiStyleVar.ItemSpacing]                  = s.ItemSpacing.X;      valsY[(int)SharedImGuiStyleVar.ItemSpacing]                  = s.ItemSpacing.Y;
        vals[(int)SharedImGuiStyleVar.ItemInnerSpacing]             = s.ItemInnerSpacing.X; valsY[(int)SharedImGuiStyleVar.ItemInnerSpacing]             = s.ItemInnerSpacing.Y;
        vals[(int)SharedImGuiStyleVar.IndentSpacing]                = s.IndentSpacing;
        vals[(int)SharedImGuiStyleVar.CellPadding]                  = s.CellPadding.X;      valsY[(int)SharedImGuiStyleVar.CellPadding]                  = s.CellPadding.Y;
        vals[(int)SharedImGuiStyleVar.ScrollbarSize]                = s.ScrollbarSize;
        vals[(int)SharedImGuiStyleVar.ScrollbarRounding]            = s.ScrollbarRounding;
        vals[(int)SharedImGuiStyleVar.GrabMinSize]                  = s.GrabMinSize;
        vals[(int)SharedImGuiStyleVar.GrabRounding]                 = s.GrabRounding;
        vals[(int)SharedImGuiStyleVar.ImageBorderSize]              = s.ImageBorderSize;
        vals[(int)SharedImGuiStyleVar.TabRounding]                  = s.TabRounding;
        vals[(int)SharedImGuiStyleVar.TabBorderSize]                = s.TabBorderSize;
        vals[(int)SharedImGuiStyleVar.TabMinWidthBase]              = s.TabMinWidthBase;
        vals[(int)SharedImGuiStyleVar.TabMinWidthShrink]            = s.TabMinWidthShrink;
        vals[(int)SharedImGuiStyleVar.TabBarBorderSize]             = s.TabBarBorderSize;
        vals[(int)SharedImGuiStyleVar.TabBarOverlineSize]           = s.TabBarOverlineSize;
        vals[(int)SharedImGuiStyleVar.TableAngledHeadersAngle]      = s.TableAngledHeadersAngle;
        vals[(int)SharedImGuiStyleVar.TableAngledHeadersTextAlign]  = s.TableAngledHeadersTextAlign.X; valsY[(int)SharedImGuiStyleVar.TableAngledHeadersTextAlign] = s.TableAngledHeadersTextAlign.Y;
        vals[(int)SharedImGuiStyleVar.TreeLinesSize]                = s.TreeLinesSize;
        vals[(int)SharedImGuiStyleVar.TreeLinesRounding]            = s.TreeLinesRounding;
        vals[(int)SharedImGuiStyleVar.ButtonTextAlign]              = s.ButtonTextAlign.X;  valsY[(int)SharedImGuiStyleVar.ButtonTextAlign]              = s.ButtonTextAlign.Y;
        vals[(int)SharedImGuiStyleVar.SelectableTextAlign]          = s.SelectableTextAlign.X; valsY[(int)SharedImGuiStyleVar.SelectableTextAlign]       = s.SelectableTextAlign.Y;
        vals[(int)SharedImGuiStyleVar.SeparatorTextBorderSize]      = s.SeparatorTextBorderSize;
        vals[(int)SharedImGuiStyleVar.SeparatorTextAlign]           = s.SeparatorTextAlign.X;  valsY[(int)SharedImGuiStyleVar.SeparatorTextAlign]        = s.SeparatorTextAlign.Y;
        vals[(int)SharedImGuiStyleVar.SeparatorTextPadding]         = s.SeparatorTextPadding.X; valsY[(int)SharedImGuiStyleVar.SeparatorTextPadding]     = s.SeparatorTextPadding.Y;
        vals[(int)SharedImGuiStyleVar.DockingSeparatorSize]         = s.DockingSeparatorSize;
    }

    private static void ApplyGlobalStyle(SharedImGuiConfig config, ImGuiStylePtr style)
    {
        switch (config.BaseTheme)
        {
            case 1: ImGui.StyleColorsDark();    break;
            case 2: ImGui.StyleColorsLight();   break;
            case 3: ImGui.StyleColorsClassic(); break;
        }

        foreach (var cmd in config.GlobalStyle)
        {
            switch (cmd)
            {
                case PushStyleColorCommand col:
                    style.Colors[(int)col.Col] = new Vector4(col.R, col.G, col.B, col.A);
                    break;

                case PushStyleVarCommand sv:
                    switch (sv.Var)
                    {
                        case SharedImGuiStyleVar.Alpha:                    style.Alpha                    = sv.Value; break;
                        case SharedImGuiStyleVar.DisabledAlpha:            style.DisabledAlpha            = sv.Value; break;
                        case SharedImGuiStyleVar.WindowRounding:           style.WindowRounding           = sv.Value; break;
                        case SharedImGuiStyleVar.WindowBorderSize:         style.WindowBorderSize         = sv.Value; break;
                        case SharedImGuiStyleVar.ChildRounding:            style.ChildRounding            = sv.Value; break;
                        case SharedImGuiStyleVar.ChildBorderSize:          style.ChildBorderSize          = sv.Value; break;
                        case SharedImGuiStyleVar.PopupRounding:            style.PopupRounding            = sv.Value; break;
                        case SharedImGuiStyleVar.PopupBorderSize:          style.PopupBorderSize          = sv.Value; break;
                        case SharedImGuiStyleVar.FrameRounding:            style.FrameRounding            = sv.Value; break;
                        case SharedImGuiStyleVar.FrameBorderSize:          style.FrameBorderSize          = sv.Value; break;
                        case SharedImGuiStyleVar.IndentSpacing:            style.IndentSpacing            = sv.Value; break;
                        case SharedImGuiStyleVar.ScrollbarSize:            style.ScrollbarSize            = sv.Value; break;
                        case SharedImGuiStyleVar.ScrollbarRounding:        style.ScrollbarRounding        = sv.Value; break;
                        case SharedImGuiStyleVar.GrabMinSize:              style.GrabMinSize              = sv.Value; break;
                        case SharedImGuiStyleVar.GrabRounding:             style.GrabRounding             = sv.Value; break;
                        case SharedImGuiStyleVar.ImageBorderSize:          style.ImageBorderSize          = sv.Value; break;
                        case SharedImGuiStyleVar.TabRounding:              style.TabRounding              = sv.Value; break;
                        case SharedImGuiStyleVar.TabBorderSize:            style.TabBorderSize            = sv.Value; break;
                        case SharedImGuiStyleVar.TabMinWidthBase:          style.TabMinWidthBase          = sv.Value; break;
                        case SharedImGuiStyleVar.TabMinWidthShrink:        style.TabMinWidthShrink        = sv.Value; break;
                        case SharedImGuiStyleVar.TabBarBorderSize:         style.TabBarBorderSize         = sv.Value; break;
                        case SharedImGuiStyleVar.TabBarOverlineSize:       style.TabBarOverlineSize       = sv.Value; break;
                        case SharedImGuiStyleVar.TableAngledHeadersAngle:  style.TableAngledHeadersAngle  = sv.Value; break;
                        case SharedImGuiStyleVar.TreeLinesSize:            style.TreeLinesSize            = sv.Value; break;
                        case SharedImGuiStyleVar.TreeLinesRounding:        style.TreeLinesRounding        = sv.Value; break;
                        case SharedImGuiStyleVar.SeparatorTextBorderSize:  style.SeparatorTextBorderSize  = sv.Value; break;
                        case SharedImGuiStyleVar.DockingSeparatorSize:     style.DockingSeparatorSize     = sv.Value; break;
                    }
                    break;

                case PushStyleVarVec2Command sv2:
                    var v = new Vector2(sv2.X, sv2.Y);
                    switch (sv2.Var)
                    {
                        case SharedImGuiStyleVar.WindowPadding:               style.WindowPadding               = v; break;
                        case SharedImGuiStyleVar.WindowMinSize:               style.WindowMinSize               = v; break;
                        case SharedImGuiStyleVar.WindowTitleAlign:            style.WindowTitleAlign            = v; break;
                        case SharedImGuiStyleVar.FramePadding:                style.FramePadding                = v; break;
                        case SharedImGuiStyleVar.ItemSpacing:                 style.ItemSpacing                 = v; break;
                        case SharedImGuiStyleVar.ItemInnerSpacing:            style.ItemInnerSpacing            = v; break;
                        case SharedImGuiStyleVar.CellPadding:                 style.CellPadding                 = v; break;
                        case SharedImGuiStyleVar.TableAngledHeadersTextAlign: style.TableAngledHeadersTextAlign = v; break;
                        case SharedImGuiStyleVar.ButtonTextAlign:             style.ButtonTextAlign             = v; break;
                        case SharedImGuiStyleVar.SelectableTextAlign:         style.SelectableTextAlign         = v; break;
                        case SharedImGuiStyleVar.SeparatorTextAlign:          style.SeparatorTextAlign          = v; break;
                        case SharedImGuiStyleVar.SeparatorTextPadding:        style.SeparatorTextPadding        = v; break;
                    }
                    break;
            }
        }
    }
}
