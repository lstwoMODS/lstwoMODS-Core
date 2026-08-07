using System;
using lstwoMODS.ImGui.Shared;
using UnityEngine;

namespace lstwoMODS_Core.Hotkeys
{
    public class OverlayHotkey
    {
        public string          Id          { get; }
        public string          DisplayName { get; }
        public KeyCode         Key         { get; internal set; }
        public HotkeyModifiers Modifiers   { get; internal set; }
        /// <summary>When true, this hotkey is hidden from the rebind panel and cannot be user-rebound.</summary>
        public bool            Excluded    { get; }
        /// <summary>When true, only the in-game input path fires this hotkey: pressing the key while
        /// the overlay window has focus does nothing. Use it for binds that act on the game world,
        /// where a press aimed at the overlay's own UI shouldn't reach through.</summary>
        public bool            GameOnly    { get; }
        public Action          OnPressed   { get; }

        public OverlayHotkey(string id, string displayName, KeyCode key,
            HotkeyModifiers modifiers, Action onPressed, bool excluded = false, bool gameOnly = false)
        {
            Id          = id;
            DisplayName = displayName;
            Key         = key;
            Modifiers   = modifiers;
            OnPressed   = onPressed;
            Excluded    = excluded;
            GameOnly    = gameOnly;
        }
    }
}
