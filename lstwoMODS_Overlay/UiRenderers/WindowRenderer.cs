using System.Numerics;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class WindowRenderer : UIRenderer
{
    public string WindowTitle { get; set; }
    public List<BaseUIElementData> Children { get; set; }
    public bool Open;
    public bool ShowCloseButton;

    private Hexa.NET.ImGui.ImGuiWindowFlags _flags;
    private float? _nextSizeX, _nextSizeY;
    private float? _nextPosX, _nextPosY;
    private float? _contentSizeX, _contentSizeY;
    private float _pivotX, _pivotY;
    private ImGuiCond _sizeCond, _posCond;
    private uint? _dockId;
    private ImGuiCond _dockCond;
    private bool _pinToMainViewport;
    private int _lastRenderFrame = -1;
    private bool _renderedLastCall;     // true if last Render() actually called Begin()
    private bool _focused;              // ImGui.IsWindowFocused this frame
    private bool _lastFocusedSent;      // last value the mod side has been told
    private bool _emitJustOpened;       // pending one-shot JustOpened pulse
    private int  _appliedFocusVersion;  // last FocusRequestVersion acted on
    private bool _focusPending;          // request to bring this window to front next render

    public WindowRenderer(BaseUIElementData data) : base(data)
    {
        var d       = data as WindowData;
        Open            = d.Open;
        ShowCloseButton = d.ShowCloseButton;
        WindowTitle     = d.WindowTitle;
        Children        = d.Children;
        _flags          = (Hexa.NET.ImGui.ImGuiWindowFlags)(int)d.WindowFlags;
        CopyNextFromData(d);
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d       = data as WindowData;
        Data            = d;
        Name            = d.Name;
        WindowTitle     = d.WindowTitle;
        if (d.Children?.Count > 0) Children = d.Children;
        Open            = d.Open;
        ShowCloseButton = d.ShowCloseButton;
        _flags          = (Hexa.NET.ImGui.ImGuiWindowFlags)(int)d.WindowFlags;
        CopyNextFromData(d);
    }

    private void CopyNextFromData(WindowData d)
    {
        _nextSizeX = d.NextSizeX; _nextSizeY = d.NextSizeY; _sizeCond = (ImGuiCond)(int)d.SizeCond;
        _nextPosX  = d.NextPosX;  _nextPosY  = d.NextPosY;  _posCond  = (ImGuiCond)(int)d.PosCond;
        _pivotX    = d.PivotX;    _pivotY    = d.PivotY;
        _contentSizeX = d.ContentSizeX; _contentSizeY = d.ContentSizeY;
        _dockId   = d.DockId;     _dockCond  = (ImGuiCond)(int)d.DockCond;
        _pinToMainViewport = d.PinToMainViewport;

        // Focus request: act once per new version (survives repeated syncs where the flag stays set).
        if (d.FocusRequested && d.FocusRequestVersion != _appliedFocusVersion)
        {
            _focusPending = true;
            _appliedFocusVersion = d.FocusRequestVersion;
        }
    }

    // A closed window renders nothing; its subtree must not count toward input capture.
    public override bool ParticipatesInInput => Open;

    public override void Render()
    {
        var currentFrame = ImGui.GetFrameCount();

        if (!Open)
        {
            _lastRenderFrame = currentFrame;
            _renderedLastCall = false;
            _focused = false;
            return;
        }

        // A gap > 1 frame means the parent container was disabled (F2 hide).
        var reappearing = _lastRenderFrame >= 0 && currentFrame > _lastRenderFrame + 1;
        _lastRenderFrame = currentFrame;

        // "Just opened" covers: initial render, menu/X reopen (Open false→true),
        // and F2 reappear (parent container gap). Mod side uses this to fire
        // OnOpenChanged exactly once per show.
        if (!_renderedLastCall || reappearing)
            _emitJustOpened = true;

        // Capture the focus target for OnRender() to apply after all Begin/End pairs (so every
        // window in the dock node has registered before the focus call overrides ImGui's pick).
        // On F2 reappear, restore the tab that was front-most before the hide; a reappearing
        // window hasn't overwritten LastSelectedWindowTitle yet (see the guard below), so it
        // still holds the pre-hide value.
        if (reappearing)
            Window.FocusTargetTitle = Window.LastSelectedWindowTitle;

        // Programmatic focus request (e.g. switching tabs) targets this exact window and wins
        // over the reappear restore if both happen this frame.
        if (_focusPending)
        {
            Window.FocusTargetTitle = WindowTitle;
            _focusPending = false;
        }

        // SetNext* must be called immediately before Begin()
        if (_nextSizeX.HasValue && _nextSizeY.HasValue)
        {
            ImGui.SetNextWindowSize(new Vector2(_nextSizeX.Value, _nextSizeY.Value), _sizeCond);
            if (_sizeCond != ImGuiCond.Always) _nextSizeX = _nextSizeY = null;
        }
        if (_nextPosX.HasValue && _nextPosY.HasValue)
        {
            float posX, posY;

            if (_pinToMainViewport)
            {
                // When pinned, positive coords are offsets from the main viewport's top-left,
                // negative coords are offsets from the bottom-right edges. Pivot controls how
                // the window aligns to the anchor (use pivot 0,1 for bottom-left anchoring).
                var mv = ImGui.GetMainViewport();
                ImGui.SetNextWindowViewport(mv.ID);
                posX = _nextPosX.Value >= 0f
                    ? mv.Pos.X + _nextPosX.Value
                    : mv.Pos.X + mv.Size.X + _nextPosX.Value;
                posY = _nextPosY.Value >= 0f
                    ? mv.Pos.Y + _nextPosY.Value
                    : mv.Pos.Y + mv.Size.Y + _nextPosY.Value;
            }
            else
            {
                // A negative value on either axis is a sentinel meaning "use display centre for that axis".
                // Centred() stores (-1,-1); CenteredX(y) stores (-1, y).
                // When a sentinel is present, pin the window to the main viewport so it never
                // drifts onto a secondary monitor in multi-viewport setups.
                var hasSentinel = _nextPosX.Value < 0f || _nextPosY.Value < 0f;
                if (hasSentinel)
                {
                    var mv = ImGui.GetMainViewport();
                    ImGui.SetNextWindowViewport(mv.ID);
                    posX = _nextPosX.Value < 0f ? mv.Pos.X + mv.Size.X / 2f : mv.Pos.X + _nextPosX.Value;
                    posY = _nextPosY.Value < 0f ? mv.Pos.Y + mv.Size.Y / 2f : mv.Pos.Y + _nextPosY.Value;
                }
                else
                {
                    posX = _nextPosX.Value;
                    posY = _nextPosY.Value;
                }
            }

            ImGui.SetNextWindowPos(new Vector2(posX, posY), _posCond, new Vector2(_pivotX, _pivotY));
            if (_posCond != ImGuiCond.Always) _nextPosX = _nextPosY = null;
        }
        if (_contentSizeX.HasValue)
        {
            ImGui.SetNextWindowContentSize(new Vector2(_contentSizeX.Value, _contentSizeY.GetValueOrDefault(0f)));
            _contentSizeX = _contentSizeY = null;
        }
        if (_dockId.HasValue)
            ImGui.SetNextWindowDockID(_dockId.Value, _dockCond);

        var began = ShowCloseButton
            ? ImGui.Begin(WindowTitle, ref Open, _flags)
            : ImGui.Begin(WindowTitle, _flags);

        IsOnMainViewport = ImGui.GetWindowViewport().ID == ImGui.GetMainViewport().ID;
        _focused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);

        if (began)
        {
            // Track which tab is selected so we can restore it after F2 hide/show.
            // Skip on the reopen frame: auto-select may have picked the wrong window,
            // and we want to preserve the pre-hide value until SetWindowFocus() corrects it.
            if (!reappearing)
                Window.LastSelectedWindowTitle = WindowTitle;

            foreach (var child in Children)
                Window.RenderSingleElement(child);
        }

        ImGui.End();
        _renderedLastCall = true;
    }

    public override BaseUIElementData? GetNewState()
    {
        var d = (Data as WindowData)!;
        var openChanged = Open != d.Open;
        var focusedChanged = _focused != _lastFocusedSent;
        if (!openChanged && !focusedChanged && !_emitJustOpened) return null;

        d.Open = Open;
        var emittingJustOpened = _emitJustOpened;
        _emitJustOpened = false;
        _lastFocusedSent = _focused;

        return new WindowData
        {
            Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled,
            WindowTitle = WindowTitle, Children = Children,
            Open = Open, ShowCloseButton = ShowCloseButton, WindowFlags = d.WindowFlags,
            JustOpened = emittingJustOpened, Focused = _focused,
            PinToMainViewport = _pinToMainViewport
        };
    }
}