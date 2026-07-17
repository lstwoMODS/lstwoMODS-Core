using System;
using System.Collections.Generic;

namespace lstwoMODS_Core.Macros;

/// <summary>
/// A named collection of macros persisted to its own file (<c>macros/groups/{Id}.json</c>)
/// so a whole group can be shared as a single file  copy the file to another install's macro
/// groups folder and it shows up as a new group on load.
///
/// Exactly one group is the <see cref="IsDefault"/> group: it always exists, can't be deleted,
/// and is where newly created macros land unless a specific group is chosen. A shared file is
/// always imported as a normal group (the receiving side collapses any extra default flags).
/// </summary>
public class MacroGroup
{
    /// <summary>Stable identity and file-name stem; survives restarts and renames. The file
    /// name is the source of truth, so this is overwritten with it on load.</summary>
    public string Id = Guid.NewGuid().ToString();

    public string Name = "New Group";

    /// <summary>The undeletable catch-all group. Persisted so its identity survives restarts;
    /// <see cref="MacroManager"/> guarantees exactly one exists.</summary>
    public bool IsDefault;

    /// <summary>Sort position among groups; the default group always sorts first regardless.</summary>
    public int Order;

    /// <summary>The macros in this group. Slugs stay globally unique across every group.</summary>
    public List<Macro> Macros = new();
}
