using System;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

/// <summary>
/// Key-combination capture button. By default the user must click it to start listening: the
/// label then shows "Press a key..." until they press a combination, which fires
/// <see cref="OnCaptured"/> and stops listening. Call <see cref="Reset"/> when your capture UI
/// opens to show the current binding (and clear any prior session). Use
/// <see cref="WithAlwaysListen"/> for the legacy behaviour where it captures continuously
/// without a click.
/// </summary>
public class KeyCapture : BaseUIElement<KeyCapture>
{
    public Action<ImGuiKey, HotkeyModifiers>? OnCaptured;

    private int _lastVersion;

    public KeyCapture(string name, Action<ImGuiKey, HotkeyModifiers> onCaptured = null, bool mainThread = true) : base(name)
    {
        Data = new KeyCaptureData { Name = name };
        OnCaptured = onCaptured;
        RunCallbacksOnMainThread = mainThread;
    }

    /// <summary>Capture continuously without requiring a click (the label shows the listening
    /// text the whole time). Default: click the button to listen.</summary>
    public KeyCapture WithAlwaysListen(bool always = true)
    { ((KeyCaptureData)Data).AlwaysListen = always; return this; }

    /// <summary>Label shown while listening for a key press. Default "Press a key...".</summary>
    public KeyCapture WithListeningText(string text)
    { ((KeyCaptureData)Data).ListeningText = text; return this; }

    /// <summary>Idle label shown when not listening (e.g. the current binding).</summary>
    public KeyCapture WithDisplay(string idleText)
    { ((KeyCaptureData)Data).DisplayText = idleText; return this; }

    /// <summary>Start a fresh session: show <paramref name="idleText"/> and stop listening
    /// (unless <see cref="WithAlwaysListen"/> is set). Call when your capture UI opens.</summary>
    public void Reset(string idleText)
    {
        var d = (KeyCaptureData)Data;
        d.DisplayText = string.IsNullOrEmpty(idleText) ? "Set key..." : idleText;
        d.ResetVersion++;
        MarkChanged();
    }

    /// <summary>Stop listening (e.g. when the capture UI closes). Same as <see cref="Reset"/>
    /// but keeps the current idle label.</summary>
    public void Stop()
    {
        var d = (KeyCaptureData)Data;
        d.ResetVersion++;
        MarkChanged();
    }

    public override void ApplyReceivedData(BaseUIElementData data)
    {
        // Config below is mod-owned; the overlay response never carries it, so keep our values
        // (base swaps in the received object wholesale).
        var old        = (KeyCaptureData)Data;
        var always     = old.AlwaysListen;
        var listening  = old.ListeningText;
        var reset      = old.ResetVersion;

        base.ApplyReceivedData(data);

        var d = (KeyCaptureData)Data;
        d.AlwaysListen  = always;
        d.ListeningText = listening;
        d.ResetVersion  = reset;

        if (d.CaptureVersion == _lastVersion || d.CapturedKey == 0) return;
        _lastVersion = d.CaptureVersion;

        var key  = (ImGuiKey)d.CapturedKey;
        var mods = (HotkeyModifiers)d.CapturedModifiers;
        InvokeCallback(() => OnCaptured?.Invoke(key, mods));
    }
}
