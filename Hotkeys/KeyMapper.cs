using System.Collections.Generic;
using UnityEngine;
using lstwoMODS.ImGui.Shared;

namespace lstwoMODS_Core.Hotkeys
{
    /// <summary>
    /// Translates between Unity's <see cref="KeyCode"/> (what the game polls) and
    /// <see cref="ImGuiKey"/> (what the overlay reports). Public so mods binding their own hotkeys
    /// through <see cref="lstwoMODS_Core.UI.Elements.KeyCapture"/> can share one table instead of
    /// each carrying a partial copy.
    ///
    /// Anything the two enums do not both name is unmappable, and that is a real limit rather than a
    /// gap to fill: Unity's legacy Input has no <c>KeyCode</c> above <c>F15</c>, so F16 to F24 can be
    /// captured by the overlay but never polled in game. Those return <see cref="KeyCode.None"/>.
    /// </summary>
    public static class KeyMapper
    {
        private static readonly Dictionary<KeyCode, ImGuiKey> _map = new()
        {
            // Letters
            { KeyCode.A, ImGuiKey.A }, { KeyCode.B, ImGuiKey.B }, { KeyCode.C, ImGuiKey.C },
            { KeyCode.D, ImGuiKey.D }, { KeyCode.E, ImGuiKey.E }, { KeyCode.F, ImGuiKey.F },
            { KeyCode.G, ImGuiKey.G }, { KeyCode.H, ImGuiKey.H }, { KeyCode.I, ImGuiKey.I },
            { KeyCode.J, ImGuiKey.J }, { KeyCode.K, ImGuiKey.K }, { KeyCode.L, ImGuiKey.L },
            { KeyCode.M, ImGuiKey.M }, { KeyCode.N, ImGuiKey.N }, { KeyCode.O, ImGuiKey.O },
            { KeyCode.P, ImGuiKey.P }, { KeyCode.Q, ImGuiKey.Q }, { KeyCode.R, ImGuiKey.R },
            { KeyCode.S, ImGuiKey.S }, { KeyCode.T, ImGuiKey.T }, { KeyCode.U, ImGuiKey.U },
            { KeyCode.V, ImGuiKey.V }, { KeyCode.W, ImGuiKey.W }, { KeyCode.X, ImGuiKey.X },
            { KeyCode.Y, ImGuiKey.Y }, { KeyCode.Z, ImGuiKey.Z },

            // Numbers
            { KeyCode.Alpha0, ImGuiKey.Key0 }, { KeyCode.Alpha1, ImGuiKey.Key1 }, { KeyCode.Alpha2, ImGuiKey.Key2 },
            { KeyCode.Alpha3, ImGuiKey.Key3 }, { KeyCode.Alpha4, ImGuiKey.Key4 }, { KeyCode.Alpha5, ImGuiKey.Key5 },
            { KeyCode.Alpha6, ImGuiKey.Key6 }, { KeyCode.Alpha7, ImGuiKey.Key7 }, { KeyCode.Alpha8, ImGuiKey.Key8 },
            { KeyCode.Alpha9, ImGuiKey.Key9 },

            // Function keys. Unity's legacy Input stops at F15, so F16-F24 have no counterpart.
            { KeyCode.F1, ImGuiKey.F1 }, { KeyCode.F2, ImGuiKey.F2 }, { KeyCode.F3, ImGuiKey.F3 },
            { KeyCode.F4, ImGuiKey.F4 }, { KeyCode.F5, ImGuiKey.F5 }, { KeyCode.F6, ImGuiKey.F6 },
            { KeyCode.F7, ImGuiKey.F7 }, { KeyCode.F8, ImGuiKey.F8 }, { KeyCode.F9, ImGuiKey.F9 },
            { KeyCode.F10, ImGuiKey.F10 }, { KeyCode.F11, ImGuiKey.F11 }, { KeyCode.F12, ImGuiKey.F12 },
            { KeyCode.F13, ImGuiKey.F13 }, { KeyCode.F14, ImGuiKey.F14 }, { KeyCode.F15, ImGuiKey.F15 },

            // Numeric keypad
            { KeyCode.Keypad0, ImGuiKey.Keypad0 }, { KeyCode.Keypad1, ImGuiKey.Keypad1 },
            { KeyCode.Keypad2, ImGuiKey.Keypad2 }, { KeyCode.Keypad3, ImGuiKey.Keypad3 },
            { KeyCode.Keypad4, ImGuiKey.Keypad4 }, { KeyCode.Keypad5, ImGuiKey.Keypad5 },
            { KeyCode.Keypad6, ImGuiKey.Keypad6 }, { KeyCode.Keypad7, ImGuiKey.Keypad7 },
            { KeyCode.Keypad8, ImGuiKey.Keypad8 }, { KeyCode.Keypad9, ImGuiKey.Keypad9 },
            { KeyCode.KeypadPeriod, ImGuiKey.KeypadDecimal }, { KeyCode.KeypadDivide, ImGuiKey.KeypadDivide },
            { KeyCode.KeypadMultiply, ImGuiKey.KeypadMultiply }, { KeyCode.KeypadMinus, ImGuiKey.KeypadSubtract },
            { KeyCode.KeypadPlus, ImGuiKey.KeypadAdd }, { KeyCode.KeypadEnter, ImGuiKey.KeypadEnter },
            { KeyCode.KeypadEquals, ImGuiKey.KeypadEqual },

            // Navigation
            { KeyCode.UpArrow, ImGuiKey.UpArrow }, { KeyCode.DownArrow, ImGuiKey.DownArrow },
            { KeyCode.LeftArrow, ImGuiKey.LeftArrow }, { KeyCode.RightArrow, ImGuiKey.RightArrow },
            { KeyCode.Return, ImGuiKey.Enter }, { KeyCode.Escape, ImGuiKey.Escape },
            { KeyCode.Backspace, ImGuiKey.Backspace }, { KeyCode.Tab, ImGuiKey.Tab },
            { KeyCode.Space, ImGuiKey.Space },
            { KeyCode.Insert, ImGuiKey.Insert }, { KeyCode.Delete, ImGuiKey.Delete },
            { KeyCode.Home, ImGuiKey.Home }, { KeyCode.End, ImGuiKey.End },
            { KeyCode.PageUp, ImGuiKey.PageUp }, { KeyCode.PageDown, ImGuiKey.PageDown },

            // Locks and system keys. Unity spells these Numlock and Print.
            { KeyCode.CapsLock, ImGuiKey.CapsLock }, { KeyCode.ScrollLock, ImGuiKey.ScrollLock },
            { KeyCode.Numlock, ImGuiKey.NumLock }, { KeyCode.Pause, ImGuiKey.Pause },
            { KeyCode.Print, ImGuiKey.PrintScreen }, { KeyCode.Menu, ImGuiKey.Menu },

            // Modifiers, for binding one on its own rather than as part of a combination.
            // KeyCode.LeftCommand / LeftApple are aliases of each other and are deliberately left
            // out: they share a numeric value, so listing both would throw on a duplicate key.
            { KeyCode.LeftShift, ImGuiKey.LeftShift }, { KeyCode.RightShift, ImGuiKey.RightShift },
            { KeyCode.LeftControl, ImGuiKey.LeftCtrl }, { KeyCode.RightControl, ImGuiKey.RightCtrl },
            { KeyCode.LeftAlt, ImGuiKey.LeftAlt }, { KeyCode.RightAlt, ImGuiKey.RightAlt },
            { KeyCode.LeftWindows, ImGuiKey.LeftSuper }, { KeyCode.RightWindows, ImGuiKey.RightSuper },

            // Punctuation
            { KeyCode.Semicolon, ImGuiKey.Semicolon }, { KeyCode.Equals, ImGuiKey.Equal },
            { KeyCode.Comma, ImGuiKey.Comma }, { KeyCode.Minus, ImGuiKey.Minus },
            { KeyCode.Period, ImGuiKey.Period }, { KeyCode.Slash, ImGuiKey.Slash },
            { KeyCode.BackQuote, ImGuiKey.GraveAccent }, { KeyCode.LeftBracket, ImGuiKey.LeftBracket },
            { KeyCode.Backslash, ImGuiKey.Backslash }, { KeyCode.RightBracket, ImGuiKey.RightBracket },
            { KeyCode.Quote, ImGuiKey.Apostrophe },
        };

        /// <summary>ImGui equivalent of a Unity key, or <see cref="ImGuiKey.None"/> when there isn't one.</summary>
        public static ImGuiKey ToImGui(KeyCode key) =>
            _map.TryGetValue(key, out var imgui) ? imgui : ImGuiKey.None;

        private static readonly Dictionary<ImGuiKey, KeyCode> _reverse = BuildReverse();

        private static Dictionary<ImGuiKey, KeyCode> BuildReverse()
        {
            var reverse = new Dictionary<ImGuiKey, KeyCode>();
            foreach (var pair in _map)
                if (!reverse.ContainsKey(pair.Value))
                    reverse[pair.Value] = pair.Key;
            return reverse;
        }

        /// <summary>Reverse mapping for the key-capture widget. <see cref="KeyCode.None"/>
        /// when the ImGui key has no Unity equivalent in the map.</summary>
        public static KeyCode ToKeyCode(ImGuiKey key) =>
            _reverse.TryGetValue(key, out var keyCode) ? keyCode : KeyCode.None;
    }
}