using System.Numerics;
using System.Text;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;
using ImGuiKey = lstwoMODS.ImGui.Shared.ImGuiKey;
using HexaKey = Hexa.NET.ImGui.ImGuiKey;

namespace lstwoMODS_Overlay.UiRenderers;

public class KeyCaptureRenderer : UIRenderer
{
    private string _display;        // idle label, or the captured combo once one exists
    private string _listeningText;
    private bool   _alwaysListen;
    private bool   _contentWidth;   // size to CalcItemWidth instead of stretching the row
    private string _label;          // drawn after the button, ImGui's usual label position
    private bool   _listening;      // overlay-local: toggled by clicking the button
    private bool   _hasCaptured;    // a combo was captured this session (show it, not the prompt)
    private int    _appliedReset;

    private ImGuiKey        _capturedKey;
    private HotkeyModifiers _capturedMods;
    private int _version;
    private int _lastReportedVersion;

    public KeyCaptureRenderer(BaseUIElementData data) : base(data)
    {
        var d = (KeyCaptureData)data;
        Name           = d.Name;
        _display       = d.DisplayText;
        _listeningText = d.ListeningText;
        _alwaysListen  = d.AlwaysListen;
        _contentWidth  = d.UseContentWidth;
        _label         = d.Label;
        _appliedReset  = d.ResetVersion;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (KeyCaptureData)data;
        Data = d; Name = d.Name;
        _listeningText = d.ListeningText;
        _alwaysListen  = d.AlwaysListen;
        _contentWidth  = d.UseContentWidth;
        _label         = d.Label;

        // A fresh session: stop listening, forget the captured combo, show the idle text.
        if (d.ResetVersion != _appliedReset)
        {
            _appliedReset = d.ResetVersion;
            _listening    = false;
            _hasCaptured  = false;
            _display      = d.DisplayText;
            _capturedKey  = ImGuiKey.None;
            _capturedMods = HotkeyModifiers.None;
        }
    }

    public override void Render()
    {
        if (_listening || _alwaysListen)
            Capture();

        // Show the listening prompt only while waiting for the first press; once a combo is
        // captured (or when idle) show _display, which holds either the idle text or the combo.
        var listening = _listening || _alwaysListen;
        var label = listening && !_hasCaptured ? _listeningText : _display;

        // A slot width from the layout wins; otherwise content width lines the button up with the
        // inputs around it, and the default stretches it across the row.
        var slotWidth = RenderContext.SlotWidth;
        var size = slotWidth > 0f
            ? new Vector2(slotWidth, 0f)
            : _contentWidth
                ? new Vector2(ImGui.CalcItemWidth(), 0f)
                : new Vector2(-float.Epsilon, 0f);

        if (ImGui.Button($"{label}###keycapture-{Data.Id}", size) && !_alwaysListen)
            _listening = !_listening;   // click toggles listening

        if (string.IsNullOrEmpty(_label)) return;

        ImGui.SameLine();
        ImGui.Text(_label);
    }

    private void Capture()
    {
        // Keyboard keys only: skip the modifier keys themselves (they're the combo's
        // modifiers, not its primary key) and everything past the gamepad/mouse range.
        for (var key = ImGuiKey.NamedKeyBegin; key < ImGuiKey.GamepadStart; key++)
        {
            if (key >= ImGuiKey.LeftCtrl && key <= ImGuiKey.RightSuper) continue;
            if (!ImGui.IsKeyPressed((HexaKey)(int)key, false)) continue;

            var mods = HotkeyModifiers.None;
            if (ImGui.IsKeyDown(HexaKey.LeftCtrl)  || ImGui.IsKeyDown(HexaKey.RightCtrl))  mods |= HotkeyModifiers.Ctrl;
            if (ImGui.IsKeyDown(HexaKey.LeftShift) || ImGui.IsKeyDown(HexaKey.RightShift)) mods |= HotkeyModifiers.Shift;
            if (ImGui.IsKeyDown(HexaKey.LeftAlt)   || ImGui.IsKeyDown(HexaKey.RightAlt))   mods |= HotkeyModifiers.Alt;

            _capturedKey  = key;
            _capturedMods = mods;
            _version++;
            _display     = FormatCombo(key, mods);
            _hasCaptured = true;
            _listening   = false;   // one-shot in click mode; always-listen keeps capturing
            return;
        }
    }

    private static string FormatCombo(ImGuiKey key, HotkeyModifiers mods)
    {
        var sb = new StringBuilder();
        if ((mods & HotkeyModifiers.Ctrl)  != 0) sb.Append("Ctrl+");
        if ((mods & HotkeyModifiers.Shift) != 0) sb.Append("Shift+");
        if ((mods & HotkeyModifiers.Alt)   != 0) sb.Append("Alt+");
        var name = key.ToString();
        if (name.StartsWith("Key") && name.Length == 4) name = name.Substring(3); // Key0 → 0
        sb.Append(name);
        return sb.ToString();
    }

    public override BaseUIElementData? GetNewState()
    {
        if (_version == _lastReportedVersion) return null;
        _lastReportedVersion = _version;

        var d = (KeyCaptureData)Data;
        d.CapturedKey       = (int)_capturedKey;
        d.CapturedModifiers = (int)_capturedMods;
        d.CaptureVersion    = _version;
        return new KeyCaptureData
        {
            Id                = Data.Id,
            Name              = Data.Name,
            Enabled           = Data.Enabled,
            DisplayText       = _display,
            ListeningText     = _listeningText,
            AlwaysListen      = _alwaysListen,
            UseContentWidth   = _contentWidth,
            Label             = _label,
            ResetVersion      = _appliedReset,
            CapturedKey       = (int)_capturedKey,
            CapturedModifiers = (int)_capturedMods,
            CaptureVersion    = _version,
        };
    }
}
