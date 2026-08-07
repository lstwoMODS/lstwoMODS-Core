using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.Messages;
using lstwoMODS_Core.UI;
using UnityEngine;

namespace lstwoMODS_Core.Hotkeys
{
    public class OverlayHotkeyManager(OSWindow window)
    {
        private readonly Dictionary<string, OverlayHotkey>       _hotkeys       = new Dictionary<string, OverlayHotkey>();
        private readonly Dictionary<string, ConfigEntry<string>>  _configEntries = new Dictionary<string, ConfigEntry<string>>();
        private readonly object _lock = new object();
        private readonly OSWindow _window = window;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Register a hotkey. If a saved binding exists in config it overrides
        /// <paramref name="defaultKey"/>/<paramref name="defaultModifiers"/>.
        ///
        /// With <c>gameOnly</c> the hotkey fires only from the in-game input path: a press that lands
        /// on the overlay window is ignored, so a bind that acts on the game world doesn't reach
        /// through while the user is working in the overlay's own UI.
        /// </summary>
        public void Register(
            string          id,
            string          displayName,
            KeyCode         defaultKey,
            HotkeyModifiers defaultModifiers,
            Action          onPressed,
            bool            excluded = false,
            bool            gameOnly = false)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("id must not be null or empty", nameof(id));

            var key  = defaultKey;
            var mods = defaultModifiers;

            var entry = Plugin.ConfigFile.Bind(
                "Hotkeys", id,
                SerializeBinding(defaultKey, defaultModifiers),
                $"Keybind for: {displayName}");

            if (TryParseBinding(entry.Value, out var savedKey, out var savedMods))
            {
                key  = savedKey;
                mods = savedMods;
            }

            // Game-only hotkeys never take the overlay path, so a missing mapping costs them nothing.
            if (!gameOnly && KeyMapper.ToImGui(key) == ImGuiKey.None)
                UnityEngine.Debug.LogWarning($"[OverlayHotkeyManager] Key '{key}' for hotkey '{id}' has no GLFW mapping  overlay input path will not fire for this hotkey.");

            lock (_lock)
            {
                _hotkeys[id]       = new OverlayHotkey(id, displayName, key, mods, onPressed, excluded, gameOnly);
                _configEntries[id] = entry;
            }

            Sync();
        }

        /// <summary>Remove a registered hotkey.</summary>
        public void Unregister(string id)
        {
            lock (_lock)
            {
                _hotkeys.Remove(id);
                _configEntries.Remove(id);
            }
            Sync();
        }

        /// <summary>
        /// Change the binding for a registered hotkey and persist to config.
        /// Used by the future rebind panel. No-ops if <paramref name="id"/> is not registered.
        /// </summary>
        public void Rebind(string id, KeyCode key, HotkeyModifiers modifiers)
        {
            lock (_lock)
            {
                if (!_hotkeys.TryGetValue(id, out var hk)) return;
                hk.Key       = key;
                hk.Modifiers = modifiers;
                if (_configEntries.TryGetValue(id, out var entry))
                    entry.Value = SerializeBinding(key, modifiers);
            }
            Sync();
        }

        /// <summary>
        /// Returns a snapshot of all registered hotkeys.
        /// Non-excluded entries are intended to be shown in the rebind panel.
        /// </summary>
        public OverlayHotkey[] GetAll()
        {
            return Snapshot();
        }

        // ── Internal ─────────────────────────────────────────────────────────

        /// <summary>Called from Plugin.Update()  handles the Unity Input path.</summary>
        internal void Update()
        {
            // When the game isn't focused the overlay window has keyboard focus;
            // the overlay's KeyPressMessage path handles hotkeys in that case.
            // Skipping here prevents double-fire (Unity raw input + overlay IPC both detecting the same key).
            if (!Application.isFocused) return;

            var snapshot = Snapshot();

            bool ctrlHeld  = Input.GetKey(KeyCode.LeftControl)  || Input.GetKey(KeyCode.RightControl);
            bool shiftHeld = Input.GetKey(KeyCode.LeftShift)     || Input.GetKey(KeyCode.RightShift);
            bool altHeld   = Input.GetKey(KeyCode.LeftAlt)       || Input.GetKey(KeyCode.RightAlt);

            var currentMods = HotkeyModifiers.None;
            if (ctrlHeld)  currentMods |= HotkeyModifiers.Ctrl;
            if (shiftHeld) currentMods |= HotkeyModifiers.Shift;
            if (altHeld)   currentMods |= HotkeyModifiers.Alt;

            foreach (var hk in snapshot)
            {
                if (!Input.GetKeyDown(hk.Key))   continue;
                if (hk.Modifiers != currentMods) continue;
                hk.OnPressed?.Invoke();
            }
        }

        /// <summary>
        /// Called from UIManager's IPC thread when the overlay sends a KeyPressMessage.
        /// Matching callbacks are enqueued onto the main thread.
        /// Note: the OS delivers keypresses to exactly one focused window at a time, so
        /// this path and <see cref="Update"/> are mutually exclusive per keypress  no
        /// double-fire is possible.
        /// </summary>
        internal void HandleOverlayKey(ImGuiKey imguiKey, HotkeyModifiers modifiers)
        {
            var snapshot = Snapshot();

            foreach (var hk in snapshot)
            {
                if (hk.GameOnly)                           continue;
                if (KeyMapper.ToImGui(hk.Key) != imguiKey) continue;
                if (hk.Modifiers             != modifiers) continue;
                var captured = hk;
                MainThread.Enqueue(() => captured.OnPressed?.Invoke());
            }
        }

        /// <summary>
        /// Sends the current set of watched GLFW primary keys to the overlay.
        /// No-ops if the IPC channel is not yet connected.
        /// Called automatically on Register/Unregister/Rebind and on overlay init.
        /// </summary>
        internal void Sync()
        {
            if (UIManager.IpcChannel == null) return;

            ImGuiKey[] imguiKeys;
            lock (_lock)
            {
                var keys = new HashSet<ImGuiKey>();
                
                // Game-only hotkeys are dropped here rather than filtered on arrival: the overlay
                // has no reason to watch a key it must never report. A key shared with a non-game-only
                // hotkey still makes the list through that one.
                foreach (var hk in _hotkeys.Values)
                {
                    if (hk.GameOnly) continue;
                    var g = KeyMapper.ToImGui(hk.Key);
                    if (g != ImGuiKey.None) keys.Add(g);
                }
                
                imguiKeys = new ImGuiKey[keys.Count];
                keys.CopyTo(imguiKeys);
            }

            UIManager.IpcChannel.SendMessage(new SetHotkeysMessage { WindowId = _window.Id, ImGuiKeys = imguiKeys }.Serialize());
        }

        // ── Persistence helpers ───────────────────────────────────────────────

        private string SerializeBinding(KeyCode key, HotkeyModifiers mods)
        {
            var parts = new List<string>();
            if ((mods & HotkeyModifiers.Ctrl)  != 0) parts.Add("Ctrl");
            if ((mods & HotkeyModifiers.Shift) != 0) parts.Add("Shift");
            if ((mods & HotkeyModifiers.Alt)   != 0) parts.Add("Alt");
            parts.Add(key.ToString());
            return string.Join("+", parts);
        }

        private bool TryParseBinding(string value, out KeyCode key, out HotkeyModifiers mods)
        {
            key  = KeyCode.None;
            mods = HotkeyModifiers.None;
            if (string.IsNullOrEmpty(value)) return false;

            var parts = value.Split('+');
            if (!Enum.TryParse(parts[parts.Length - 1].Trim(), out key)) return false;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                switch (parts[i].Trim().ToLowerInvariant())
                {
                    case "ctrl":  mods |= HotkeyModifiers.Ctrl;  break;
                    case "shift": mods |= HotkeyModifiers.Shift; break;
                    case "alt":   mods |= HotkeyModifiers.Alt;   break;
                }
            }
            return true;
        }

        private OverlayHotkey[] Snapshot()
        {
            lock (_lock)
            {
                var arr = new OverlayHotkey[_hotkeys.Count];
                _hotkeys.Values.CopyTo(arr, 0);
                return arr;
            }
        }
    }
}
