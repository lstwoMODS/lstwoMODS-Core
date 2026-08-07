using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using lstwoMODS_Core.Hotkeys;
using lstwoMODS_Core.UI.Elements;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.Messages;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI;

public abstract class OSWindow : IDisposable
{
    public string Id = Guid.NewGuid().ToString();
    public string Title;
    public int Width, Height;
    public WindowType WindowType;
    public IntPtr  FollowWindowHandle;
    public string  Backend;
    public OverlayHotkeyManager HotkeyManager;

    public Dictionary<int, BaseUIElement> Elements = new();

    private readonly Queue<CreatedElementEntry> _createdElements = new();
    private readonly HashSet<int>               _pendingCreatedIds = new();
    private readonly Queue<int>                 _deletedElements = new();
    private readonly object                  _lock             = new();

    private ImGuiConfig          _config = new();
    private bool                 _initialized;
    private List<FontDescriptor> _fonts  = new();

    /// <summary>
    /// Get or set the ImGui IO configuration for this window.
    /// If set after Initialize() has been called, sends a SetImGuiConfigMessage to the overlay immediately.
    /// </summary>
    public ImGuiConfig Config
    {
        get => _config;
        set
        {
            _config = value;
            if (_initialized)
            {
                UIManager.IpcChannel.SendMessage(new SetImGuiConfigMessage
                {
                    WindowId = Id,
                    Config   = value
                }.Serialize());
            }
        }
    }


    /// <summary>Set a float style var globally (affects all ImGui, including popups and tooltips).</summary>
    public OSWindow WithGlobalStyleVar(ImGuiStyleVar var, float value)
    {
        _config.GlobalStyle.Add(new PushStyleVarCommand { Var = var, Value = value });
        return this;
    }

    /// <summary>Set a Vec2 style var globally.</summary>
    public OSWindow WithGlobalStyleVar(ImGuiStyleVar var, float x, float y)
    {
        _config.GlobalStyle.Add(new PushStyleVarVec2Command { Var = var, X = x, Y = y });
        return this;
    }

    /// <summary>Set a style color globally.</summary>
    public OSWindow WithGlobalStyleColor(ImGuiCol col, float r, float g, float b, float a)
    {
        _config.GlobalStyle.Add(new PushStyleColorCommand { Col = col, R = r, G = g, B = b, A = a });
        return this;
    }

    /// <summary>Apply all style commands from a StylePreset as permanent global style overrides.</summary>
    public OSWindow WithGlobalPreset(StylePreset preset)
    {
        _config.GlobalStyle.AddRange(preset.Commands);
        return this;
    }

    /// <summary>
    /// Register a font. Works at any time  before or after the window is initialized.
    /// If called after initialization the overlay rebuilds the font atlas immediately.
    /// </summary>
    /// <param name="name">Name used to reference this font in WithFont() calls.</param>
    /// <param name="filePath">Path relative to the overlay's working directory (e.g. "Assets/MyFont.ttf").</param>
    /// <param name="size">Font size in pixels.</param>
    /// <summary>Names of all fonts registered with this window (including those added before initialization).</summary>
    public IEnumerable<string> RegisteredFontNames => _fonts.Select(f => f.Name);

    /// <param name="merge">Merge the font's glyphs into the previously added font (icon fonts)
    /// instead of registering a standalone, pushable font.</param>
    /// <param name="glyphOffsetY">Vertical glyph offset in pixels; only meaningful with merge.</param>
    public void AddFont(string name, string filePath, float size, bool merge = false, float glyphOffsetY = 0f)
    {
        _fonts.Add(new FontDescriptor { Name = name, FilePath = filePath, Size = size, Merge = merge, GlyphOffsetY = glyphOffsetY });
        if (_initialized)
        {
            UIManager.IpcChannel.SendMessage(new RegisterFontMessage
            {
                WindowId     = Id,
                Name         = name,
                FilePath     = filePath,
                Size         = size,
                Merge        = merge,
                GlyphOffsetY = glyphOffsetY
            }.Serialize());
        }
    }

    /// <summary>
    /// Preload an image texture on the overlay side so it is ready before any UIImage element renders it.
    /// Call at any time - before or after initialization. After initialization the overlay loads immediately.
    /// </summary>
    /// <param name="filePath">Path relative to the overlay's working directory.</param>
    public void PreloadImage(string filePath)
    {
        if (_initialized)
        {
            UIManager.IpcChannel.SendMessage(new PreloadImageMessage
            {
                WindowId = Id,
                FilePath = filePath
            }.Serialize());
        }
    }

    /// <summary>
    /// Load ImGui window layout settings from a previously saved ini string.
    /// Call this at any time  before or after initialization.
    /// The overlay applies it at the start of the next render frame.
    /// Use the complementary <c>ImGuiConfig.DisableIniSave = true</c> if you want
    /// to manage layout state entirely in code rather than auto-saving to disk.
    /// </summary>
    /// <param name="iniContent">Content previously obtained from ImGui.SaveIniSettingsToMemory()
    /// or stored from a prior session.</param>
    public void LoadIniSettings(string iniContent)
    {
        if (!_initialized)
        {
            _pendingIniContent = iniContent;
            return;
        }
        UIManager.IpcChannel.SendMessage(new LoadIniSettingsMessage
        {
            WindowId   = Id,
            IniContent = iniContent
        }.Serialize());
    }

    // Held until Initialize() sends it along with the WindowInitMessage
    private string _pendingIniContent;

    protected OSWindow(string title, int width, int height, WindowType windowType, IntPtr followWindowHandle, string backend = null)
    {
        Title = title;
        Width = width;
        Height = height;
        WindowType = windowType;
        FollowWindowHandle = followWindowHandle;
        Backend = backend;

        HotkeyManager = new(this);
    }

    public abstract void ConstructUI();

    public async Task Initialize()
    {
        ConstructUI();

        var message = new WindowInitMessage
        {
            WindowId           = Id,
            Title              = Title,
            WindowType         = WindowType,
            FollowWindowHandle = FollowWindowHandle.ToInt64(),
            Width              = Width,
            Height             = Height,
            Elements           = GetAllElementData(),
            Config             = _config,
            Fonts              = _fonts.ToArray(),
            Backend            = Backend
        };

        lock (_lock)
        {
            _createdElements.Clear();
            _pendingCreatedIds.Clear();
        }

        _initialized = true;
        UIManager.Windows[Id] = this;
        UIManager.IpcChannel.SendMessage(message.Serialize());

        if (_pendingIniContent != null)
        {
            UIManager.IpcChannel.SendMessage(new LoadIniSettingsMessage
            {
                WindowId   = Id,
                IniContent = _pendingIniContent
            }.Serialize());
            _pendingIniContent = null;
        }
    }

    /// <summary>
    /// Re-send this window's full current state to a freshly restarted overlay process.
    /// Unlike <see cref="Initialize"/>, the UI is NOT reconstructed  the live element tree
    /// (including everything added at runtime) is replayed as a new WindowInitMessage, so the
    /// user gets the exact pre-crash UI back. Layout is restored from the overlay's own
    /// imgui.ini.
    /// </summary>
    internal void Reinitialize()
    {
        if (!_initialized) return;

        IpcMessage message;
        lock (_lock)
        {
            // Pending diffs referred to the dead overlay's state  the init message below
            // carries the complete tree, so replaying them would only duplicate elements.
            _createdElements.Clear();
            _pendingCreatedIds.Clear();
            _deletedElements.Clear();
            foreach (var el in Elements.Values)
                el.WasDataChanged = false;

            message = new WindowInitMessage
            {
                WindowId           = Id,
                Title              = Title,
                WindowType         = WindowType,
                FollowWindowHandle = FollowWindowHandle.ToInt64(),
                Width              = Width,
                Height             = Height,
                Elements           = GetAllElementData(),
                Config             = _config,
                Fonts              = _fonts.ToArray(),
                Backend            = Backend
            }.Serialize();
        }

        UIManager.IpcChannel.SendMessage(message);
        HotkeyManager.Sync(); // after the init message so the overlay window exists to receive it
    }

    public void HandleFrameRequest(FrameRequestMessage request)
    {
        IpcMessage msg;
        int createdCount = 0, updatedCount = 0, removedCount = 0;
        BaseUIElementData[] debugUpdated = null;

        var sw = Plugin.DeveloperModeEntry?.Value == true ? Stopwatch.StartNew() : null;

        lock (_lock)
        {
            foreach (var outputData in request.OutputElements)
            {
                if (Elements.TryGetValue(outputData.Id, out var element))
                    element.ApplyReceivedData(outputData);
            }

            var created = DrainQueue(_createdElements);
            _pendingCreatedIds.Clear();

            var changed = Elements.Values
                .Where(x => x.WasDataChanged)
                .ToArray();

            var updated = changed
                .Select(x => x.Data)
                .ToArray();

            foreach (var el in changed)
                el.WasDataChanged = false;

            var removed = DrainQueue(_deletedElements);

            createdCount  = created.Length;
            updatedCount  = updated.Length;
            removedCount  = removed.Length;
            debugUpdated  = sw != null ? updated : null;

            msg = new FrameStateMessage
            {
                WindowId          = Id,
                CreatedElements   = created,
                UpdatedElements   = updated,
                RemovedElementIds = removed
            }.Serialize();
        }

        sw?.Stop();

        UIManager.IpcChannel.SendMessage(msg);
    }

    public BaseUIElementData[] GetAllElementData()
    {
        return Elements.Values
            .Where(x => x.IsTopLevel)
            .Select(x => x.Data)
            .ToArray();
    }

    /// <summary>Add an element (and its subtree) at the window's top level at runtime.</summary>
    public BaseUIElement AddElement(BaseUIElement element) => AddElement(element, null);

    /// <summary>
    /// Add an element (and its subtree) at runtime. With <paramref name="parent"/> set, the
    /// element is inserted into the parent's children and renders inside it; the parent must
    /// be a container-like element (Group, Container, ChildWindow, CollapsingHeader, TreeNode,
    /// Modal, GuiWindow, ...) that is already part of this window.
    /// </summary>
    /// <param name="index">Insert position within the parent's children. -1 = append.</param>
    public BaseUIElement AddElement(BaseUIElement element, BaseUIElement parent, int index = -1)
    {
        lock (_lock)
        {
            if (parent != null)
            {
                if (!Elements.ContainsKey(parent.Data.Id))
                    throw new InvalidOperationException(
                        $"Parent \"{parent.Name}\" is not part of window \"{Title}\".");
                if (!parent.InsertChildAt(element, index))
                    throw new InvalidOperationException(
                        $"{parent.GetType().Name} \"{parent.Name}\" cannot hold children.");
            }

            RegisterElement(element, parent);

            // Created entries hold a live reference to element.Data and are serialized at
            // frame-send time. If an ancestor's create is still queued, this subtree already
            // rides along inside the ancestor's Children  queueing it again would make the
            // overlay create it twice.
            var ancestorPending = false;
            for (var p = parent; p != null; p = p.Parent)
            {
                if (!_pendingCreatedIds.Contains(p.Data.Id)) continue;
                ancestorPending = true;
                break;
            }

            if (!ancestorPending)
            {
                _createdElements.Enqueue(new CreatedElementEntry
                {
                    ParentId = parent?.Data.Id ?? -1,
                    Index    = index,
                    Data     = element.Data,
                });
            }
            _pendingCreatedIds.Add(element.Data.Id);
        }
        return element;
    }

    private void RegisterElement(BaseUIElement element, BaseUIElement? parent = null)
    {
        element.Parent = parent;
        Elements.Add(element.Data.Id, element);
        foreach (var child in element.GetChildren())
            RegisterElement(child, element);
    }

    /// <summary>
    /// Remove an element (and its subtree) at runtime  works for top-level elements and
    /// for children added via <see cref="AddElement(BaseUIElement, BaseUIElement, int)"/>
    /// or built into a container at construction time.
    /// </summary>
    public void RemoveElement(BaseUIElement element)
    {
        lock (_lock)
        {
            element.Parent?.RemoveChildElement(element);
            UnregisterElement(element);
            _deletedElements.Enqueue(element.Data.Id);
        }
    }

    private void UnregisterElement(BaseUIElement element)
    {
        element.Parent = null;
        Elements.Remove(element.Data.Id);
        foreach (var child in element.GetChildren())
            UnregisterElement(child);
    }

    public void FocusGameWindow()
    {
        UIManager.IpcChannel.SendMessage(new FocusGameWindowMessage
        {
            WindowId = Id
        }.Serialize());
    }

    /// <summary>
    /// Ask the overlay to bring its own OS window to the foreground and grab keyboard focus.
    /// Use for auto-focusing UI (e.g. a chat input) so the user can type immediately without
    /// having to click the overlay first.
    /// </summary>
    public void FocusOverlayWindow()
    {
        UIManager.IpcChannel.SendMessage(new FocusOverlayWindowMessage
        {
            WindowId = Id
        }.Serialize());
    }

    public void Dispose()
    {
        UIManager.Windows.Remove(Id);
    }

    private static T[] DrainQueue<T>(Queue<T> queue)
    {
        var items = new T[queue.Count];
        for (var i = 0; i < items.Length; i++)
            items[i] = queue.Dequeue();
        return items;
    }
}
