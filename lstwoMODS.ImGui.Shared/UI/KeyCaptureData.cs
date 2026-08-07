namespace lstwoMODS.ImGui.Shared.UI
{
    /// <summary>
    /// Key-combination capture button. By default it listens only after the user clicks the
    /// button (the label switches to <see cref="ListeningText"/> while it waits); the next
    /// non-modifier key pressed, together with the Ctrl/Shift/Alt state, is reported back
    /// (bumping <see cref="CaptureVersion"/>) and listening stops. Set <see cref="AlwaysListen"/>
    /// to capture continuously without a click.
    /// </summary>
    public class KeyCaptureData : BaseUIElementData
    {
        /// <summary>Idle label shown when not listening (e.g. the current binding).</summary>
        public string DisplayText { get; set; } = "Set key...";

        /// <summary>Label shown while listening for a key press.</summary>
        public string ListeningText { get; set; } = "Press a key...";

        /// <summary>Mod→overlay. When true, capture continuously without requiring a click.</summary>
        public bool AlwaysListen { get; set; } = false;

        /// <summary>
        /// Mod→overlay. When true the button width matches <c>ImGui.CalcItemWidth()</c> instead of
        /// stretching across the row, so it lines up with the sliders and inputs around it.
        /// </summary>
        public bool UseContentWidth { get; set; } = false;

        /// <summary>
        /// Mod→overlay. Drawn after the button on the same line, which is where ImGui puts the
        /// label of every other input. Null or empty draws nothing.
        /// </summary>
        public string Label { get; set; }

        /// <summary>Mod→overlay. Bump to start a fresh session: stop listening and show
        /// <see cref="DisplayText"/> again (forgetting any locally shown captured combo).</summary>
        public int ResetVersion { get; set; }

        /// <summary>Last captured primary key as an <see cref="ImGuiKey"/> value. 0 = none.</summary>
        public int CapturedKey { get; set; }

        /// <summary><see cref="HotkeyModifiers"/> bits held when the key was pressed.</summary>
        public int CapturedModifiers { get; set; }

        /// <summary>Incremented by the renderer for every new capture so the mod side can
        /// detect repeat captures of the same combination.</summary>
        public int CaptureVersion { get; set; }
    }
}
