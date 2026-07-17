using System.Collections.Generic;

namespace lstwoMODS.ImGui.Shared.UI
{
    public class InputTextData : BaseUIElementData
    {
        public string Value { get; set; } = "";
        public string Hint { get; set; } = "";
        public int MaxLength { get; set; } = 256;
        public bool Multiline { get; set; } = false;
        public float SizeX { get; set; } = -1f;
        public float SizeY { get; set; } = 100f;
        public ImGuiInputTextFlags Flags { get; set; } = ImGuiInputTextFlags.None;

        /// <summary>
        /// ImGuiKey values the renderer should report back when pressed while this input has
        /// keyboard focus. Used for chat-style Tab-complete / Up-Down history navigation.
        /// </summary>
        public List<int> WatchKeys { get; set; } = new List<int>();

        /// <summary>
        /// ImGuiKey of the last watched key the renderer observed. Paired with <see cref="LastKeyVersion"/>
        /// so the consumer can detect a key edge even when the same key fires twice in a row.
        /// 0 = none.
        /// </summary>
        public int LastKeyPressed { get; set; }
        public int LastKeyVersion { get; set; }

        /// <summary>Request the renderer to grab keyboard focus on the next frame.</summary>
        public bool RequestFocus { get; set; }
        public int RequestFocusVersion { get; set; }

        /// <summary>True if the renderer's InputText currently has keyboard focus.</summary>
        public bool IsFocused { get; set; }
    }
}
