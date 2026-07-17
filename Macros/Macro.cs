using System;
using System.Collections.Generic;

namespace lstwoMODS_Core.Macros;

/// <summary>
/// A user-defined macro: a trigger plus a list of steps executed in order.
/// </summary>
public class Macro
{
    /// <summary>Stable identity that survives restarts and renames. Step/trigger
    /// references and hotkey ids are keyed on this, never on the name.</summary>
    public string Id = Guid.NewGuid().ToString();

    public string Name = "New Macro";

    /// <summary>Short unique call id ("farm_loop") for referencing this macro from other
    /// macros and expressions. Derived from the name once (deduplicated by
    /// <see cref="MacroManager"/>) and deliberately NOT updated on rename, so stored
    /// references and expression strings never break.</summary>
    public string Slug;

    public bool Enabled = true;

    /// <summary>Simple mode hides every per-parameter mode selector (so all arguments are
    /// plain constants), the target/context rows and the output-name rows, leaving just a
    /// value to set per step  the quick path for "on this key, set speed to 7, noclip on".
    /// Advanced mode restores the full editor. Defaults to <c>false</c> on the serialized
    /// field so macros saved before this existed keep their advanced editor; new macros are
    /// created simple by <see cref="MacroManager.Add"/>. A simple macro never holds advanced
    /// value sources  switching in flattens them (see the editor's simple-mode warning).</summary>
    public bool Simple;

    public MacroTrigger Trigger = new();

    /// <summary>Steps run when the macro fires (the "on" list in hotkey Toggle mode).</summary>
    public List<MacroStep> Steps = new();

    /// <summary>Steps run on the second press in hotkey Toggle mode. Unused otherwise.</summary>
    public List<MacroStep> OffSteps = new();
}
