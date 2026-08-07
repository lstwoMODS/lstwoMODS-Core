using System;
using lstwoMODS.ImGui.Shared;
using lstwoMODS_Core.Hotkeys;
using lstwoMODS_Core.Macros;
using lstwoMODS_Core.UI.Elements;
using UnityEngine;

namespace lstwoMODS_Core.UI;

/// <summary>
/// Owns the single shared "Add to macro" / "Create hotkey" dialog used by the right-click
/// context menus that <see cref="ModContextMenu"/> builds. The modal is mounted once by
/// <see cref="LstwoModsPanels"/> and opened imperatively from anywhere via
/// <see cref="OpenCreateMacro"/> / <see cref="OpenCreateHotkey"/>.
///
/// Both flows create a macro through the public <see cref="MacroManager"/> API. A "hotkey"
/// is just a simple macro with a <see cref="MacroTriggerBuiltins.HotkeyId"/> trigger, so it reuses
/// all of the macro system's persistence and startup re-arming; there is no separate hotkey
/// store. After creation the user is sent to the Macros window to configure the macro further.
/// </summary>
public static class ModContextMenuService
{
    private enum Mode { Macro, Hotkey }

    private static Mode   _mode;
    private static string _methodId;
    // True when the target is a bool setting: a "toggle keybind" then flips the value each
    // press. For anything else, "toggle keybind" uses the macro's on/off (Toggle) trigger mode.
    private static bool   _boolSetting;

    private static ImGuiKey       _capturedKey  = ImGuiKey.None;
    private static HotkeyModifiers _capturedMods = HotkeyModifiers.None;

    private static Modal      _modal;
    private static InputText  _nameBox;
    private static readonly Ref<string> _name = new("");
    private static KeyCapture _keyCapture;
    private static Checkbox   _toggleBox;
    private static Container  _hotkeyRow;   // key capture + toggle, hidden in Macro mode

    /// <summary>Build the shared modal element tree. Call once and mount the returned element
    /// in an always-rendered container (see <see cref="LstwoModsPanels"/>).</summary>
    public static BaseUIElement BuildUI()
    {
        if (_modal != null) return _modal;

        _nameBox = new InputText("##ModCtx-name", hint: "Name...", maxLength: 64)
            .WithValue(_name)
            .WithItemWidth(260f);

        _keyCapture = new KeyCapture("ModCtx-capture", OnKeyCaptured);
        _toggleBox  = new Checkbox("Toggle keybind", false)
            .WithTooltip("On: for a bool setting each press flips it; for anything else the macro runs its "
                       + "on-steps on the first press and off-steps on the second.");

        _hotkeyRow = new Container("ModCtx-hotkey",
            new Separator("ModCtx-hk-sep"),
            new UIText("ModCtx-hk-label", "Hotkey"),
            _keyCapture,
            new Spacing("ModCtx-hk-sp0"),
            _toggleBox,
            new Spacing("ModCtx-hk-sp1"));

        _modal = new Modal("ModCtx-modal", "Add to Macro",
            _nameBox,
            new Spacing("ModCtx-sp-name"),
            _hotkeyRow,
            new Button("Create##ModCtx-create", Confirm).WithItemWidth(80f),
            new SameLine("ModCtx-sl"),
            new Button("Cancel##ModCtx-cancel", Cancel).WithItemWidth(80f)
        ).OnClose(() => _keyCapture?.Stop());

        return _modal;
    }

    /// <summary>Open the dialog to create a new macro seeded with a single step for
    /// <paramref name="methodId"/> (a <see cref="MacroRegistry"/> id, e.g. from
    /// <see cref="MacroManager.MethodIdFor(Hacks.ModActionDescriptor)"/>).</summary>
    public static void OpenCreateMacro(string methodId, string defaultName)
    {
        if (!Prepare(Mode.Macro, methodId, defaultName, boolSetting: false)) return;
        _hotkeyRow.SetVisible(false);
        _modal.SetTitle("Add to Macro");
        _modal.Open();
        _nameBox.FocusNextFrame();
    }

    /// <summary>Open the dialog to create a hotkey (a simple macro with a hotkey trigger) that
    /// runs <paramref name="methodId"/>. <paramref name="boolSetting"/> makes the toggle option
    /// flip the value each press rather than use the macro's on/off mode.</summary>
    public static void OpenCreateHotkey(string methodId, string defaultName, bool boolSetting)
    {
        if (!Prepare(Mode.Hotkey, methodId, defaultName, boolSetting)) return;
        _toggleBox.Value = false;
        _capturedKey  = ImGuiKey.None;
        _capturedMods = HotkeyModifiers.None;
        _hotkeyRow.SetVisible(true);
        _keyCapture.Reset("Set key...");
        _modal.SetTitle("Create Hotkey");
        _modal.Open();
        _nameBox.FocusNextFrame();
    }

    private static bool Prepare(Mode mode, string methodId, string defaultName, bool boolSetting)
    {
        if (_modal == null || string.IsNullOrEmpty(methodId)) return false;
        _mode        = mode;
        _methodId    = methodId;
        _boolSetting = boolSetting;
        _name.Value  = string.IsNullOrWhiteSpace(defaultName) ? "New Macro" : defaultName;
        return true;
    }

    private static void OnKeyCaptured(ImGuiKey key, HotkeyModifiers mods)
    {
        if (key == ImGuiKey.Escape) return;   // Escape cancels the capture, not the dialog
        _capturedKey  = key;
        _capturedMods = mods;
    }

    private static void Cancel()
    {
        _keyCapture.Stop();
        _modal.Close();
    }

    private static void Confirm()
    {
        KeyCode keyCode = KeyCode.None;
        if (_mode == Mode.Hotkey)
        {
            if (_capturedKey == ImGuiKey.None) return;
            keyCode = KeyMapper.ToKeyCode(_capturedKey);
            if (keyCode == KeyCode.None) return;
        }

        var macro = MacroManager.Add(_name.Value);
        var step  = MacroManager.AddStep(macro, _methodId);

        if (_mode == Mode.Hotkey)
        {
            var toggle = _toggleBox.Value;

            macro.Trigger.TypeId = MacroTriggerBuiltins.HotkeyId;
            macro.Trigger.Set(MacroTriggerBuiltins.BindingKey, new HotkeyBinding(keyCode, _capturedMods).ToString());
            macro.Trigger.Set(MacroTriggerBuiltins.ModeKey, toggle ? MacroHotkeyMode.Toggle : MacroHotkeyMode.Press);

            if (toggle && _boolSetting && step != null)
            {
                step.SetArg("value", new ConstantValueSource { Value = MacroValues.ToDisplay(true) });
                var offStep = MacroManager.AddStep(macro, _methodId, off: true);
                offStep?.SetArg("value", new ConstantValueSource { Value = MacroValues.ToDisplay(false) });
            }

            _keyCapture.Stop();
            MacroManager.NotifyTriggerChanged();
        }

        _modal.Close();

        LstwoModsPanels.RevealWindow(LstwoModsPanels.MacrosWindow);
    }
}
