using System;
using System.Collections.Generic;
using System.Linq;

namespace lstwoMODS_Core.Macros;

/// <summary>
/// One way of choosing a value of a registered macro type: an entry in the parameter
/// mode dropdown (e.g. for a player: "Local Player", "First Player", "By Name").
/// </summary>
public class MacroTypeMode
{
    /// <summary>Stable id persisted in macro JSON.</summary>
    public string Id;
    /// <summary>Shown in the parameter mode dropdown.</summary>
    public string Label;

    /// <summary>Optional single argument (e.g. the player name for "By Name").
    /// Null = parameterless mode with no editor at all.</summary>
    public MacroParam Param;

    /// <summary>Resolve the value at run time. Receives <c>[argValue]</c> when
    /// <see cref="Param"/> is set, else an empty array. Throw with a clear message
    /// when the object can't be resolved; the runner reports it as a step failure.</summary>
    public Func<object[], object> Resolve;

    /// <summary>Optional live options for <see cref="Param"/>; the editor shows them as a
    /// dropdown (e.g. the names of the players currently in the game).</summary>
    public Func<string[]> Choices;
}

/// <summary>
/// Describes how macro parameters of <see cref="Type"/> are selected and displayed.
/// Registered by game plugins for their object types (players, vehicles, NPCs, ...).
/// </summary>
public class MacroTypeDescriptor
{
    public Type Type;
    /// <summary>Short name for parameter labels ("Player").</summary>
    public string DisplayName;

    public List<MacroTypeMode> Modes = new();

    /// <summary>Mode preselected for new steps (e.g. "local").</summary>
    public string DefaultModeId;

    /// <summary>Optional string coercion that lets constants, expression results and step
    /// outputs of type string resolve to the object (e.g. a player name). Registered
    /// into <see cref="MacroValues"/> automatically.</summary>
    public Func<string, object> ResolveFromString;

    /// <summary>Maps a resolved value to the key used for detached mod instances
    /// (e.g. PlayerRef → its PlayerController, so equal players share an instance).
    /// Default: the value itself.</summary>
    public Func<object, object> ContextCacheKey = v => v;

    public MacroTypeMode FindMode(string id) => Modes.FirstOrDefault(m => m.Id == id);
    public MacroTypeMode DefaultMode => FindMode(DefaultModeId) ?? Modes.FirstOrDefault();
}

/// <summary>
/// Registry of macro object types. Game plugins register their types here; the macro
/// editor turns each type's modes into parameter mode-dropdown entries, and the runner
/// resolves them via <see cref="TypedModeValueSource"/>.
/// </summary>
public static class MacroTypes
{
    private static readonly Dictionary<Type, MacroTypeDescriptor> _byType = new();

    public static void Register(MacroTypeDescriptor descriptor)
    {
        if (descriptor?.Type == null) throw new ArgumentException("MacroTypeDescriptor needs a Type.");
        _byType[descriptor.Type] = descriptor;
        if (descriptor.ResolveFromString != null)
            MacroValues.RegisterResolver(descriptor.Type, descriptor.ResolveFromString);
    }

    public static MacroTypeDescriptor For(Type type)
        => type != null && _byType.TryGetValue(type, out var d) ? d : null;

    /// <summary>Resolve a registered type by its <see cref="MacroTypeDescriptor.DisplayName"/> or the
    /// CLR type name (case-insensitive), e.g. "Player" -> PlayerRef. Null when nothing matches. Used
    /// to parse a human-typed type name in a macro parameter spec ("target:Player").</summary>
    public static Type ByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        name = name.Trim();
        var match = _byType.Values.FirstOrDefault(d =>
            string.Equals(d.DisplayName, name, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(d.Type.Name, name, StringComparison.OrdinalIgnoreCase));
        return match?.Type;
    }

    // ── Selection strings ─────────────────────────────────────────────────
    // A pick made through a type's modes, flattened to one string ("local", "byName:Bob") so it
    // can live in a string-only store  a trigger's config dictionary. Step arguments keep the
    // fields apart in a TypedModeValueSource instead; this is the compact form of the same thing.

    /// <summary>Encode a mode pick. An empty <paramref name="modeId"/> encodes "unset".</summary>
    public static string EncodeSelection(string modeId, string arg)
        => string.IsNullOrEmpty(modeId) ? ""
         : string.IsNullOrEmpty(arg) ? modeId
         : $"{modeId}:{arg}";

    /// <summary>Split a selection string; <paramref name="modeId"/> is null when nothing is
    /// stored. Only the first colon splits, so an arg may itself contain colons.</summary>
    public static void DecodeSelection(string stored, out string modeId, out string arg)
    {
        modeId = null;
        arg = null;
        if (string.IsNullOrEmpty(stored)) return;
        var at = stored.IndexOf(':');
        if (at < 0) { modeId = stored; return; }
        modeId = stored.Substring(0, at);
        arg = stored.Substring(at + 1);
    }

    /// <summary>Resolve a selection string to a live value, or null when nothing is stored.
    /// Resolves at the moment it's called (a renamed player is found under its new name), and
    /// propagates whatever the mode's <see cref="MacroTypeMode.Resolve"/> throws when it can't
    /// resolve right now  no game running, nobody by that name  so call it where a miss is
    /// handleable.</summary>
    public static object ResolveSelection(string stored, Type type)
    {
        DecodeSelection(stored, out var modeId, out var arg);
        if (modeId == null) return null;

        var descriptor = For(type)
            ?? throw new InvalidOperationException($"No macro type registered for '{type?.Name}' (plugin missing?).");
        var mode = descriptor.FindMode(modeId) ?? descriptor.DefaultMode
            ?? throw new InvalidOperationException($"Macro type '{descriptor.DisplayName}' has no selection modes.");

        var args = mode.Param != null
            ? new[] { MacroValues.Coerce(arg, mode.Param.Type) }
            : Array.Empty<object>();
        return MacroValues.Coerce(mode.Resolve(args), type);
    }

    /// <summary>Resolve a persisted type id (<c>Type.FullName</c>), or null.</summary>
    public static MacroTypeDescriptor ById(string typeId)
        => string.IsNullOrEmpty(typeId) ? null : _byType.Values.FirstOrDefault(d => d.Type.FullName == typeId);

    /// <summary>Cache key for detached mod instances, honoring the type's
    /// <see cref="MacroTypeDescriptor.ContextCacheKey"/> when registered.</summary>
    public static object CacheKeyFor(Type type, object value)
    {
        var d = For(type);
        return d != null && value != null ? d.ContextCacheKey(value) : value;
    }
}
