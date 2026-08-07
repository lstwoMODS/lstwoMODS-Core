using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace lstwoMODS_Core.Macros;

/// <summary>
/// One step of a macro: a call to a <see cref="MacroMethodDescriptor"/> with one
/// <see cref="ValueSource"/> per parameter.
/// </summary>
public class MacroStep
{
    /// <summary>Stable identity so future step-output references survive reordering.</summary>
    public string Id = Guid.NewGuid().ToString();

    /// <summary>Registry id of the method to call (see <see cref="MacroRegistry"/>).
    /// A step whose id no longer resolves (mod uninstalled) is skipped at run time.</summary>
    public string MethodId = "";

    /// <summary>One entry per method parameter, keyed by <see cref="MacroParam.Name"/> so
    /// stored values survive parameters being added or reordered in a mod update (names are
    /// assumed unique within a method). A missing entry means "use the parameter's default".</summary>
    public Dictionary<string, ValueSource> NamedArgs = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Legacy positional storage (files saved before named args). Converted by
    /// <see cref="MigrateLegacyArgs"/> on first contact with the method descriptor, then
    /// dropped from future saves.</summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<ValueSource> Args;

    /// <summary>Optional name under which this step's return value is visible to later
    /// steps' expressions (identifier characters only). Null/empty = only reachable via
    /// the Step-output dropdown or <c>prev</c>.</summary>
    public string OutputName;

    /// <summary>Sources from previously used modes, keyed <c>"{paramName}:{mode}"</c>, so
    /// switching a parameter's mode round-trips (an expression survives a trip through
    /// Value mode and comes back as typed). Persisted with the macro.</summary>
    public Dictionary<string, ValueSource> ArgStash = new();

    /// <summary>Free-form data bag owned by a custom step editor (see
    /// <see cref="MacroStepEditor"/>). A method with a <see cref="MacroMethodDescriptor.CustomEditor"/>
    /// stores whatever it needs here instead of using <see cref="NamedArgs"/>; read/written as a
    /// typed object through <see cref="MacroStepEditorContext.GetData{T}"/> /
    /// <see cref="MacroStepEditorContext.SetData{T}"/>. Null for ordinary steps and omitted from
    /// their JSON.</summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public JObject Custom;

    public ValueSource GetArg(string paramName)
        => !string.IsNullOrEmpty(paramName) && NamedArgs.TryGetValue(paramName, out var source) ? source : null;

    public void SetArg(string paramName, ValueSource source)
    {
        if (string.IsNullOrEmpty(paramName)) return;
        NamedArgs[paramName] = source;
    }

    /// <summary>
    /// Convert legacy positional <see cref="Args"/> into <see cref="NamedArgs"/>. Positions
    /// map onto the method's non-context parameters (the parameter list as it existed before
    /// context params were introduced), except when the saved args visibly used the short-lived
    /// context-first layout (a leading typed-mode source matching the context param's type),
    /// which maps positionally onto the full list. No-op once migrated.
    /// </summary>
    public void MigrateLegacyArgs(MacroMethodDescriptor desc)
    {
        if (Args == null || desc == null) return;

        var all = desc.Parameters ?? Array.Empty<MacroParam>();
        var contextFirst = all.Length > 0 && all[0].IsContext && Args.Count == all.Length
            && Args[0] is TypedModeValueSource tm && tm.TypeId == all[0].Type?.FullName;
        var targets = contextFirst ? all : all.Where(p => !p.IsContext).ToArray();

        for (var i = 0; i < Args.Count && i < targets.Length; i++)
            if (Args[i] != null && GetArg(targets[i].Name) == null)
                SetArg(targets[i].Name, Args[i]);

        // Stash keys were "{argIndex}:{mode}"; rekey them by the same position mapping.
        var stash = new Dictionary<string, ValueSource>();
        foreach (var kv in ArgStash)
        {
            var split = kv.Key.IndexOf(':');
            if (split > 0 && int.TryParse(kv.Key.Substring(0, split), out var index))
            {
                if (index < targets.Length)
                    stash[$"{targets[index].Name}{kv.Key.Substring(split)}"] = kv.Value;
            }
            else
            {
                stash[kv.Key] = kv.Value; // already name-keyed
            }
        }
        ArgStash = stash;

        Args = null;
    }
}
