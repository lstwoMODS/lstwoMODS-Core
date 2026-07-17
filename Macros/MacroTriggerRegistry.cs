using System;
using System.Collections.Generic;
using System.Linq;

namespace lstwoMODS_Core.Macros;

/// <summary>How a trigger config field is edited in a macro card's "Trigger Options".</summary>
public enum MacroTriggerWidget
{
    /// <summary>Pick the editor from <see cref="MacroTriggerParam.Type"/>: checkbox for bool,
    /// combo for an enum, drag field for a number, text box for anything else.</summary>
    Auto,

    /// <summary>Press-the-keys capture. The field must be a <see cref="string"/>; its stored
    /// value is a binding in "Ctrl+Shift+F" form (see <see cref="Hotkeys.HotkeyBinding"/>).</summary>
    Keybind,
}

/// <summary>
/// One configurable field of a trigger. Rendered under "Trigger Options" on the macro card and
/// stored (as a display string, via <see cref="MacroValues.ToDisplay"/>) in
/// <see cref="MacroTrigger.Config"/> under <see cref="Key"/>.
/// </summary>
public sealed class MacroTriggerParam
{
    /// <summary>Stable key persisted in the trigger's config dictionary.</summary>
    public string Key;

    /// <summary>Label shown next to the editor.</summary>
    public string Label;

    /// <summary>Drives the editor widget and how the stored string is coerced back to a value
    /// (<see cref="MacroTrigger.GetInt"/> / <see cref="MacroTrigger.GetFloat"/> / ...). A type
    /// registered with <see cref="MacroTypes"/> (Player, ...) gets that type's own selection
    /// modes  the same picker a step parameter of the type shows  and is read back with
    /// <see cref="MacroTrigger.GetTyped{T}"/>.</summary>
    public Type Type = typeof(string);

    /// <summary>Seeded into config when the trigger is first selected. For a macro-type field
    /// that's a <see cref="MacroTypes.EncodeSelection"/> string (or null/"" to start on
    /// <see cref="EmptyLabel"/>).</summary>
    public object Default;

    /// <summary>Macro-type fields only: label of a leading dropdown entry that stores nothing, for
    /// a field that's meaningfully unset  a filter that's off ("Any Player"). Null (the default)
    /// means the field must always name something.</summary>
    public string EmptyLabel;

    /// <summary>Override the auto-picked editor (e.g. a keybind capture for a string field).</summary>
    public MacroTriggerWidget Widget = MacroTriggerWidget.Auto;

    /// <summary>Optional hover text on the field label.</summary>
    public string Tooltip;
}

/// <summary>
/// One named value a trigger hands the run when it fires (which player triggered it, how much
/// money was gained, ...). Declared on <see cref="MacroTriggerDescriptor.Outputs"/> so the editor
/// knows the names and types up front: they validate in expressions as bare variables and the
/// actual values arrive through <see cref="MacroTriggerContext.Fire(ValueTuple{string,object}[])"/>.
/// A run started without a trigger (editor Play, Run Macro) sees the declared outputs as their
/// type default, so an expression referencing one never breaks.
/// </summary>
public sealed class MacroTriggerOutput
{
    /// <summary>Identifier the value is read under: a bare variable in expressions
    /// (<c>player</c>, <c>amount</c>) and the key for <c>trigger("...")</c>. Use identifier
    /// characters so it can be typed directly in an expression.</summary>
    public string Key;

    /// <summary>Human label for editor help; falls back to <see cref="Key"/> when unset.</summary>
    public string Label;

    /// <summary>Declared type, used for edit-time expression validation (so <c>player.Controller</c>
    /// type-checks). The runtime value is coerced to whatever consumes it, like any other value.</summary>
    public Type Type = typeof(object);

    /// <summary>Optional description shown in the trigger's editor help.</summary>
    public string Tooltip;
}

/// <summary>
/// Handed to a trigger's <see cref="MacroTriggerDescriptor.Arm"/>: the macro being armed, a
/// <see cref="Fire()"/> that activates it exactly as any trigger would, and typed access to the
/// trigger's stored config. A trigger never touches the macro's steps directly, only Fire().
/// </summary>
public sealed class MacroTriggerContext
{
    public MacroTriggerContext(Macro macro) => Macro = macro;

    public Macro Macro { get; }
    public MacroTrigger Trigger => Macro.Trigger;

    /// <summary>Activate the macro. Routes through <see cref="MacroManager.Fire"/> so a Toggle
    /// macro's On/Off alternation, the already-running guard and the Enabled flag all apply,
    /// no matter what fired it.</summary>
    public void Fire() => MacroManager.Fire(Macro);

    /// <summary>Activate the macro, handing the run the named values it fired with (the trigger's
    /// declared <see cref="MacroTriggerDescriptor.Outputs"/>). Each is readable in the macro's
    /// expressions as a bare variable named by its key and via <c>trigger("key")</c>. An empty list
    /// behaves like <see cref="Fire()"/>.</summary>
    public void Fire(params (string Key, object Value)[] outputs)
    {
        if (outputs == null || outputs.Length == 0) { MacroManager.Fire(Macro); return; }
        var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in outputs)
            if (!string.IsNullOrEmpty(key)) values[key] = value;
        MacroManager.Fire(Macro, values);
    }

    /// <summary>As <see cref="Fire(ValueTuple{string,object}[])"/>, for values already in a map.</summary>
    public void Fire(IReadOnlyDictionary<string, object> outputs) => MacroManager.Fire(Macro, outputs);

    public string GetString(string key)                 => Trigger.GetString(key);
    public bool   GetBool(string key)                   => Trigger.GetBool(key);
    public int    GetInt(string key)                    => Trigger.GetInt(key);
    public float  GetFloat(string key)                  => Trigger.GetFloat(key);
    public T      GetEnum<T>(string key) where T : struct => Trigger.GetEnum<T>(key);
    public T      GetTyped<T>(string key) where T : class => Trigger.GetTyped<T>(key);
}

/// <summary>
/// A kind of macro trigger. The built-in Manual and Hotkey triggers are registered through this
/// same public API (see <see cref="MacroTriggerBuiltins"/>), so a mod's trigger is in no way a
/// second-class citizen  it can do everything the built-ins do.
/// </summary>
public sealed class MacroTriggerDescriptor
{
    /// <summary>Stable id persisted in macro JSON (e.g. "core.hotkey", "wl.interval").
    /// Prefix with your mod name to avoid collisions.</summary>
    public string Id;

    /// <summary>Shown in the trigger dropdown on the macro card.</summary>
    public string Label;

    /// <summary>Config fields rendered under "Trigger Options"; empty for a no-config trigger.</summary>
    public MacroTriggerParam[] Params = Array.Empty<MacroTriggerParam>();

    /// <summary>
    /// Named values this trigger hands the run when it fires (the player who triggered it, the
    /// money gained, ...). Declared here so they validate as bare expression variables and list in
    /// the editor's help; the actual values are supplied by
    /// <see cref="MacroTriggerContext.Fire(ValueTuple{string,object}[])"/>. Empty for a trigger that
    /// carries no data (the common case).
    /// </summary>
    public MacroTriggerOutput[] Outputs = Array.Empty<MacroTriggerOutput>();

    /// <summary>
    /// Optional: compute the outputs from a configured trigger, so a trigger can expose outputs that
    /// depend on how it's set up (e.g. one per parameter of the chat command it's bound to). Return
    /// null to fall back to the static <see cref="Outputs"/>. When set, this drives the editor's
    /// "Provides" list, edit-time expression validation and the runtime binding alike  see
    /// <see cref="ResolveOutputs"/>. Leave null (the default) for a fixed output set.
    /// </summary>
    public Func<MacroTrigger, MacroTriggerOutput[]> DynamicOutputs;

    /// <summary>The effective outputs for <paramref name="trigger"/>: <see cref="DynamicOutputs"/>
    /// when it returns non-null, otherwise the static <see cref="Outputs"/>. Never null.</summary>
    public MacroTriggerOutput[] ResolveOutputs(MacroTrigger trigger)
    {
        if (trigger != null && DynamicOutputs != null)
        {
            var dynamic = DynamicOutputs(trigger);
            if (dynamic != null) return dynamic;
        }
        return Outputs;
    }

    /// <summary>
    /// Start listening. Invoke <see cref="MacroTriggerContext.Fire"/> whenever the macro should
    /// run, and return an <see cref="IDisposable"/> that stops listening (use
    /// <see cref="CallbackDisposable"/>). Return null when there is nothing to arm (Manual) or
    /// the config isn't runnable yet (e.g. no key bound). Called again on every rearm, so the
    /// returned handle must fully undo whatever Arm subscribed to.
    /// </summary>
    public Func<MacroTriggerContext, IDisposable> Arm;

    /// <summary>
    /// Whether, given its current config, this trigger drives a separate "Off" step list  an
    /// alternating on/off activation like Hotkey's Toggle mode. Controls the On/Off list selector
    /// in the editor and the toggle alternation in <see cref="MacroManager.AdvanceToggle"/>.
    /// Null means never (the common case).
    /// </summary>
    public Func<MacroTrigger, bool> UsesOffList;
}

/// <summary>An <see cref="IDisposable"/> that runs an action once when disposed. What a trigger's
/// <see cref="MacroTriggerDescriptor.Arm"/> returns to unsubscribe.</summary>
public sealed class CallbackDisposable : IDisposable
{
    private Action _onDispose;
    public CallbackDisposable(Action onDispose) => _onDispose = onDispose;

    public void Dispose()
    {
        var action = _onDispose;
        _onDispose = null; // idempotent: rearm disposes, and a double-dispose must not re-run
        action?.Invoke();
    }
}

/// <summary>
/// Database of every kind of macro trigger. Built-ins register in the static constructor; mods
/// call <see cref="Register"/> at startup (see WLMacroTriggers on the game side). The macro
/// editor lists <see cref="All"/> in the trigger dropdown and <see cref="MacroManager"/> arms
/// each macro through the matching descriptor.
/// </summary>
public static class MacroTriggerRegistry
{
    private static readonly List<MacroTriggerDescriptor> _triggers = new();

    static MacroTriggerRegistry() => MacroTriggerBuiltins.Register();

    /// <summary>Add or replace a trigger kind (last registration of an id wins).</summary>
    public static void Register(MacroTriggerDescriptor descriptor)
    {
        if (string.IsNullOrEmpty(descriptor?.Id))
            throw new ArgumentException("Trigger descriptor needs a non-empty Id.");
        _triggers.RemoveAll(t => t.Id == descriptor.Id);
        _triggers.Add(descriptor);
    }

    /// <summary>All trigger kinds, in registration order (Manual and Hotkey first).</summary>
    public static IReadOnlyList<MacroTriggerDescriptor> All => _triggers;

    /// <summary>The descriptor for an id, or null.</summary>
    public static MacroTriggerDescriptor Find(string id)
        => string.IsNullOrEmpty(id) ? null : _triggers.FirstOrDefault(t => t.Id == id);

    /// <summary>The descriptor for a trigger, falling back to Manual when the id is unknown (a
    /// trigger from a mod that's no longer installed), so an orphaned macro still shows and can
    /// be run by hand instead of vanishing.</summary>
    public static MacroTriggerDescriptor For(MacroTrigger trigger)
        => Find(trigger?.TypeId) ?? Find(MacroTriggerBuiltins.ManualId);
}
