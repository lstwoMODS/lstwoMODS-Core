using System;
using System.Collections.Generic;
using lstwoMODS.ImGui.Shared;
using UnityEngine;

namespace lstwoMODS_Core.Hotkeys;

/// <summary>
/// A key plus its modifiers, with a stable text form ("Ctrl+Shift+F") shared by
/// <see cref="OverlayHotkeyManager"/>'s config store and the built-in Hotkey macro trigger.
/// Having one parser/formatter means a persisted binding round-trips identically everywhere.
/// </summary>
public readonly struct HotkeyBinding
{
    public readonly KeyCode Key;
    public readonly HotkeyModifiers Modifiers;

    public HotkeyBinding(KeyCode key, HotkeyModifiers modifiers)
    {
        Key = key;
        Modifiers = modifiers;
    }

    /// <summary>"Ctrl+Shift+F". A binding with <see cref="KeyCode.None"/> formats as "None".</summary>
    public override string ToString()
    {
        var parts = new List<string>();
        if ((Modifiers & HotkeyModifiers.Ctrl)  != 0) parts.Add("Ctrl");
        if ((Modifiers & HotkeyModifiers.Shift) != 0) parts.Add("Shift");
        if ((Modifiers & HotkeyModifiers.Alt)   != 0) parts.Add("Alt");
        parts.Add(Key.ToString());
        return string.Join("+", parts);
    }

    /// <summary>Parse "Ctrl+Shift+F". False (and a default binding) when the key segment
    /// isn't a <see cref="KeyCode"/>; unknown modifier segments are ignored.</summary>
    public static bool TryParse(string value, out HotkeyBinding binding)
    {
        binding = default;
        if (string.IsNullOrEmpty(value)) return false;

        var parts = value.Split('+');
        if (!Enum.TryParse(parts[parts.Length - 1].Trim(), out KeyCode key)) return false;

        var mods = HotkeyModifiers.None;
        for (var i = 0; i < parts.Length - 1; i++)
        {
            switch (parts[i].Trim().ToLowerInvariant())
            {
                case "ctrl":  mods |= HotkeyModifiers.Ctrl;  break;
                case "shift": mods |= HotkeyModifiers.Shift; break;
                case "alt":   mods |= HotkeyModifiers.Alt;   break;
            }
        }
        binding = new HotkeyBinding(key, mods);
        return true;
    }
}
