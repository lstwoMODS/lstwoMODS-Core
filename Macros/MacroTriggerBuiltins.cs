using System;
using System.Collections;
using System.Collections.Generic;
using lstwoMODS_Core.Hotkeys;
using UnityEngine;

namespace lstwoMODS_Core.Macros;

/// <summary>
/// The engine's own triggers, registered through the public <see cref="MacroTriggerRegistry"/> so
/// they are exactly the kind of trigger a mod would write  nothing here uses a private path a mod
/// couldn't. Read this as the reference example for authoring your own (see also WLMacroTriggers).
/// </summary>
public static class MacroTriggerBuiltins
{
    public const string ManualId   = "core.manual";
    public const string HotkeyId   = "core.hotkey";
    public const string IntervalId = "core.interval";
    public const string CalledId   = "core.called";

    /// <summary>Config key for the Called-by-Macro trigger's parameter list.</summary>
    public const string ParamsKey = "params";

    /// <summary>Config key for the Hotkey trigger's binding string ("Ctrl+Shift+F").</summary>
    public const string BindingKey = "binding";
    /// <summary>Config key for the Hotkey trigger's <see cref="MacroHotkeyMode"/>.</summary>
    public const string ModeKey = "mode";
    /// <summary>Config key for the Hotkey trigger's "also fire while the overlay has focus" flag.
    /// Unset (the default) means the bind is in-game only.</summary>
    public const string InOverlayKey = "inOverlay";
    /// <summary>Config key for the Interval trigger's period, in seconds.</summary>
    public const string SecondsKey = "seconds";

    internal static void Register()
    {
        MacroTriggerRegistry.Register(new MacroTriggerDescriptor
        {
            Id    = ManualId,
            Label = "Manual",
            Arm   = null,
        });

        // Called by Macro: never self-arms (like Manual). It exists to DECLARE the parameters a
        // macro accepts from a caller  the "Run Macro" step reads these to show one argument
        // field per parameter, and passes the values in as this run's trigger outputs. So each
        // parameter is readable in the macro's steps as a bare variable (or trigger("name")).
        MacroTriggerRegistry.Register(new MacroTriggerDescriptor
        {
            Id    = CalledId,
            Label = "Called by Macro",
            Params = new[]
            {
                new MacroTriggerParam
                {
                    Key = ParamsKey, Label = "Parameters", Type = typeof(string), Default = "",
                    Widget = MacroTriggerWidget.ParamList,
                    Tooltip = "The parameters this macro accepts from its caller (one per row).\n"
                            + "Name each parameter, e.g. \"count\". Add a type after a colon to\n"
                            + "type-check expressions and get the right picker in the caller:\n"
                            + "\"count:int\", \"target:Player\", \"loud:bool\". Read a parameter in a\n"
                            + "step as a bare variable (count) or with trigger(\"count\").",
                },
            },
            DynamicOutputs = t => ParseParams(t?.GetString(ParamsKey)),
            Arm = null,
        });

        MacroTriggerRegistry.Register(new MacroTriggerDescriptor
        {
            Id    = HotkeyId,
            Label = "Hotkey",
            Params = new[]
            {
                new MacroTriggerParam
                {
                    Key = BindingKey, Label = "Key", Type = typeof(string),
                    Widget = MacroTriggerWidget.Keybind, Default = "",
                    Tooltip = "The key combination that fires this macro.",
                },
                new MacroTriggerParam
                {
                    Key = ModeKey, Label = "Mode", Type = typeof(MacroHotkeyMode),
                    Default = MacroHotkeyMode.Press,
                    Tooltip = "Press: every press runs the On steps.\n"
                            + "Toggle: presses alternate between the On and Off step lists.",
                },
                new MacroTriggerParam
                {
                    Key = InOverlayKey, Label = "Also in overlay", Type = typeof(bool),
                    Default = false,
                    Tooltip = "Off (the default): the key only fires this macro in game, so pressing it\n"
                            + "while you're working in the overlay does nothing.\n"
                            + "On: the key fires the macro from the overlay too.",
                },
            },
            UsesOffList = t => t.GetEnum<MacroHotkeyMode>(ModeKey) == MacroHotkeyMode.Toggle,
            Arm = ctx =>
            {
                var manager = Plugin.Window?.HotkeyManager;
                if (manager == null) return null;

                if (!HotkeyBinding.TryParse(ctx.GetString(BindingKey), out var binding)
                    || binding.Key == KeyCode.None)
                    return null;

                var id = $"macro.{ctx.Macro.Id}";
                manager.Register(id, $"Macro: {ctx.Macro.Name}", binding.Key, binding.Modifiers,
                    ctx.Fire, excluded: true, gameOnly: !ctx.GetBool(InOverlayKey));
                manager.Rebind(id, binding.Key, binding.Modifiers);

                return new CallbackDisposable(() => manager.Unregister(id));
            },
        });

        MacroTriggerRegistry.Register(new MacroTriggerDescriptor
        {
            Id    = IntervalId,
            Label = "Interval",
            Params = new[]
            {
                new MacroTriggerParam
                {
                    Key = SecondsKey, Label = "Every (s)", Type = typeof(float), Default = 5f,
                    Tooltip = "Run this macro every this many seconds while it's enabled.",
                },
            },
            Arm = ctx =>
            {
                var seconds = ctx.GetFloat(SecondsKey);
                if (seconds <= 0f) return null;

                var stopped = false;
                Plugin._StartCoroutine(Loop());
                return new CallbackDisposable(() => stopped = true);

                IEnumerator Loop()
                {
                    while (!stopped)
                    {
                        for (var elapsed = 0f; elapsed < seconds; elapsed += Time.deltaTime)
                        {
                            if (stopped) yield break;
                            yield return null;
                        }

                        if (!stopped) ctx.Fire();
                    }
                }
            },
        });
    }

    /// <summary>Parse a Called-by-Macro parameter spec ("count, target:Player, loud:bool") into
    /// trigger outputs. Each entry is <c>name</c> or <c>name:type</c>; unknown/absent types fall
    /// back to <see cref="object"/>. Blank and duplicate names are dropped.</summary>
    internal static MacroTriggerOutput[] ParseParams(string spec)
    {
        if (string.IsNullOrWhiteSpace(spec)) return Array.Empty<MacroTriggerOutput>();

        var outputs = new List<MacroTriggerOutput>();
        foreach (var entry in spec.Split(','))
        {
            var part = entry.Trim();
            if (part.Length == 0) continue;

            var name = part;
            var type = typeof(object);
            var colon = part.IndexOf(':');
            if (colon >= 0)
            {
                name = part.Substring(0, colon).Trim();
                type = ParamType(part.Substring(colon + 1).Trim());
            }

            if (name.Length == 0) continue;
            if (outputs.Exists(o => string.Equals(o.Key, name, StringComparison.OrdinalIgnoreCase))) continue;
            outputs.Add(new MacroTriggerOutput { Key = name, Label = name, Type = type });
        }
        return outputs.ToArray();
    }

    /// <summary>Map a human-typed type name to a CLR type: the common primitives, else a type
    /// registered with <see cref="MacroTypes"/> (Player, ...), else <see cref="object"/>.</summary>
    private static Type ParamType(string name)
    {
        switch ((name ?? "").ToLowerInvariant())
        {
            case "":
            case "string":
            case "text":    return typeof(string);
            case "int":
            case "integer": return typeof(int);
            case "float":
            case "double":
            case "number":  return typeof(float);
            case "bool":
            case "boolean":
            case "flag":    return typeof(bool);
            default:        return MacroTypes.ByName(name) ?? typeof(object);
        }
    }
}
