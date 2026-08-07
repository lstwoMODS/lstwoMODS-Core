namespace lstwoMODS_Core;

/// <summary>
/// Controls what the F2 hotkey toggles. Selected via the plugin config / Settings window.
/// </summary>
public enum F2Mode
{
    /// <summary>F2 toggles the menu bar and all panels together (default).</summary>
    ToggleMenuBarAndPanels,

    /// <summary>F2 toggles only the menu bar; panels stay visible (e.g. moved to a second monitor).</summary>
    ToggleMenuBarOnly,

    /// <summary>F2 does nothing; the menu bar and panels are always visible.</summary>
    ToggleNothing,
}
