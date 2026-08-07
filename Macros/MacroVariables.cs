using System;
using System.Collections.Generic;

namespace lstwoMODS_Core.Macros;

/// <summary>Where a macro variable lives, and thus who can see it. Declared Group-first so it is
/// the default value for new Set/Get steps.</summary>
public enum MacroVarScope
{
    /// <summary>Visible to every macro in the same group. The default for new steps.</summary>
    Group,
    /// <summary>Visible to every macro in every group.</summary>
    Global,
    /// <summary>Visible only to the one macro that owns it (still shared across its runs).</summary>
    Macro,
}

/// <summary>
/// A tiny in-memory key/value store shared between macros, so one macro can set a value another
/// reads. Variables are <b>session-lifetime</b>: they persist across every run for as long as the
/// game is open and reset on restart (durable values go through the Save/Load Variable steps,
/// which use <see cref="DataStorage"/>). Each variable lives in one <see cref="MacroVarScope"/>;
/// the same name in different scopes is a different variable.
///
/// The runner sets <see cref="Current"/> to the executing macro's identity for the duration of a
/// step (narrowly, like <see cref="MacroRunner.CurrentChain"/>), so the Group/Macro scopes and the
/// <c>var()</c> expression function resolve against the right macro without every caller threading
/// it through.
/// </summary>
public static class MacroVariables
{
    private static readonly object _lock = new();

    // One flat dictionary; the scope is folded into the key so there is a single store to reason
    // about. Keys: "g:{name}", "grp:{groupId}:{name}", "m:{macroId}:{name}".
    private static readonly Dictionary<string, object> _values = new(StringComparer.Ordinal);

    /// <summary>The macro currently executing (id + its group id), set per step by the runner so
    /// Group/Macro-scoped access resolves. Default (both null) outside a run  Group/Macro reads
    /// then see nothing, which is the safe no-op.</summary>
    public static (string MacroId, string GroupId) Current;

    private static string KeyFor(MacroVarScope scope, string name)
    {
        var (macroId, groupId) = Current;
        return scope switch
        {
            MacroVarScope.Global => "g:" + name,
            MacroVarScope.Group  => "grp:" + (groupId ?? "") + ":" + name,
            MacroVarScope.Macro  => "m:" + (macroId ?? "") + ":" + name,
            _                    => "g:" + name,
        };
    }

    /// <summary>Store <paramref name="value"/> under <paramref name="name"/> in the given scope.
    /// A null/empty name is ignored (a cleared editor field).</summary>
    public static void Set(MacroVarScope scope, string name, object value)
    {
        if (string.IsNullOrEmpty(name)) return;
        lock (_lock) _values[KeyFor(scope, name)] = value;
    }

    /// <summary>Read <paramref name="name"/> from exactly the given scope, or null when unset.</summary>
    public static object Get(MacroVarScope scope, string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        lock (_lock) return _values.TryGetValue(KeyFor(scope, name), out var v) ? v : null;
    }

    /// <summary>Resolve <paramref name="name"/> the way the <c>var()</c> expression function does:
    /// the most specific scope wins  Macro, then Group, then Global. Null when it is set nowhere.</summary>
    public static object Resolve(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        lock (_lock)
        {
            if (_values.TryGetValue(KeyFor(MacroVarScope.Macro, name), out var m)) return m;
            if (_values.TryGetValue(KeyFor(MacroVarScope.Group, name), out var g)) return g;
            if (_values.TryGetValue(KeyFor(MacroVarScope.Global, name), out var gl)) return gl;
            return null;
        }
    }

    /// <summary>Drop every stored variable (all scopes). Not called automatically; here for
    /// completeness and tests.</summary>
    public static void Clear()
    {
        lock (_lock) _values.Clear();
    }
}
