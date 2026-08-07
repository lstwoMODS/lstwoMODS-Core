using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using lstwoMODS.ImGui.Shared;
using lstwoMODS_Core.Hotkeys;
using UnityEngine;

namespace lstwoMODS_Core.Macros;

/// <summary>Activation style for the built-in Hotkey trigger.</summary>
public enum MacroHotkeyMode
{
    Press,
    Toggle,
}

/// <summary>Legacy trigger kind, kept only so pre-registry macro files migrate (see
/// <see cref="MacroTrigger.Migrate"/>). New code uses <see cref="MacroTrigger.TypeId"/>.</summary>
public enum MacroTriggerType { Manual, Hotkey }

/// <summary>
/// The trigger of a macro: which registered <see cref="MacroTriggerDescriptor"/> fires it
/// (<see cref="TypeId"/>) plus that trigger's own settings (<see cref="Config"/>). The engine
/// stores config as display strings so any trigger  built-in or from a mod  persists the same
/// way; use the typed getters to read it back.
/// </summary>
public class MacroTrigger
{
    /// <summary>Registry id of this trigger's kind (see <see cref="MacroTriggerRegistry"/>).
    /// Defaults to Manual.</summary>
    public string TypeId = MacroTriggerBuiltins.ManualId;

    /// <summary>Per-field config in display-string form, keyed by <see cref="MacroTriggerParam.Key"/>.</summary>
    public Dictionary<string, string> Config = new();

    [JsonProperty("Type", NullValueHandling = NullValueHandling.Ignore)]
    [JsonConverter(typeof(StringEnumConverter))]
    private MacroTriggerType? _legacyType;

    [JsonProperty("Key", NullValueHandling = NullValueHandling.Ignore)]
    [JsonConverter(typeof(StringEnumConverter))]
    private KeyCode? _legacyKey;

    [JsonProperty("Modifiers", NullValueHandling = NullValueHandling.Ignore)]
    [JsonConverter(typeof(StringEnumConverter))]
    private HotkeyModifiers? _legacyModifiers;

    [JsonProperty("Mode", NullValueHandling = NullValueHandling.Ignore)]
    [JsonConverter(typeof(StringEnumConverter))]
    private MacroHotkeyMode? _legacyMode;

    public bool Migrate()
    {
        if (_legacyType == null) return false; // already new-format

        if (_legacyType == MacroTriggerType.Hotkey)
        {
            TypeId = MacroTriggerBuiltins.HotkeyId;
            Config[MacroTriggerBuiltins.BindingKey] =
                new HotkeyBinding(_legacyKey ?? KeyCode.None, _legacyModifiers ?? HotkeyModifiers.None).ToString();
            Config[MacroTriggerBuiltins.ModeKey] = (_legacyMode ?? MacroHotkeyMode.Press).ToString();
        }
        else
        {
            TypeId = MacroTriggerBuiltins.ManualId;
        }

        _legacyType = null;
        _legacyKey = null;
        _legacyModifiers = null;
        _legacyMode = null;
        return true;
    }

    // ── Typed config access ───────────────────────────────────────────────

    /// <summary>Raw stored string for a config key, or null if unset.</summary>
    public string GetString(string key) => Config.TryGetValue(key, out var v) ? v : null;

    public bool  GetBool(string key)  => Coerce<bool>(key);
    public int   GetInt(string key)   => Coerce<int>(key);
    public float GetFloat(string key) => Coerce<float>(key);

    /// <summary>Config value parsed as an enum, or <c>default(T)</c> when unset/invalid.</summary>
    public T GetEnum<T>(string key) where T : struct
        => Enum.TryParse<T>(GetString(key), ignoreCase: true, out var v) ? v : default;

    /// <summary>Config value of a field whose type is registered with <see cref="MacroTypes"/>
    /// (a Player pick, ...), resolved live at the moment you call it. Null when the field is
    /// unset (its <see cref="MacroTriggerParam.EmptyLabel"/> entry). Unlike the other getters
    /// this propagates the type's resolve failure  no game running, nobody by that name  rather
    /// than hiding it as a default, so call it where a miss is handleable.</summary>
    public T GetTyped<T>(string key) where T : class
        => MacroTypes.ResolveSelection(GetString(key), typeof(T)) as T;

    /// <summary>Store a config value in its display-string form.</summary>
    public void Set(string key, object value) => Config[key] = MacroValues.ToDisplay(value);

    private T Coerce<T>(string key)
    {
        try { return (T)MacroValues.Coerce(GetString(key), typeof(T)); }
        catch { return default; }
    }
}
