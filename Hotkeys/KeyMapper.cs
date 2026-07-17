using System.Collections.Generic;
using UnityEngine;
using lstwoMODS.ImGui.Shared;

namespace lstwoMODS_Core.Hotkeys
{
    internal static class KeyMapper
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

            // Function keys
            { KeyCode.F1, ImGuiKey.F1 }, { KeyCode.F2, ImGuiKey.F2 }, { KeyCode.F3, ImGuiKey.F3 },
            { KeyCode.F4, ImGuiKey.F4 }, { KeyCode.F5, ImGuiKey.F5 }, { KeyCode.F6, ImGuiKey.F6 },
            { KeyCode.F7, ImGuiKey.F7 }, { KeyCode.F8, ImGuiKey.F8 }, { KeyCode.F9, ImGuiKey.F9 },
            { KeyCode.F10, ImGuiKey.F10 }, { KeyCode.F11, ImGuiKey.F11 }, { KeyCode.F12, ImGuiKey.F12 },

            // Navigation
            { KeyCode.UpArrow, ImGuiKey.UpArrow }, { KeyCode.DownArrow, ImGuiKey.DownArrow },
            { KeyCode.LeftArrow, ImGuiKey.LeftArrow }, { KeyCode.RightArrow, ImGuiKey.RightArrow },
            { KeyCode.Return, ImGuiKey.Enter }, { KeyCode.Escape, ImGuiKey.Escape },
            { KeyCode.Backspace, ImGuiKey.Backspace }, { KeyCode.Tab, ImGuiKey.Tab },
            { KeyCode.Space, ImGuiKey.Space },

            // Punctuation
            { KeyCode.Semicolon, ImGuiKey.Semicolon }, { KeyCode.Equals, ImGuiKey.Equal },
            { KeyCode.Comma, ImGuiKey.Comma }, { KeyCode.Minus, ImGuiKey.Minus },
            { KeyCode.Period, ImGuiKey.Period }, { KeyCode.Slash, ImGuiKey.Slash },
            { KeyCode.BackQuote, ImGuiKey.GraveAccent }, { KeyCode.LeftBracket, ImGuiKey.LeftBracket },
            { KeyCode.Backslash, ImGuiKey.Backslash }, { KeyCode.RightBracket, ImGuiKey.RightBracket },
            { KeyCode.Quote, ImGuiKey.Apostrophe },
        };

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