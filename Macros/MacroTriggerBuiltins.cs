using System;
using System.Collections;
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

    /// <summary>Config key for the Hotkey trigger's binding string ("Ctrl+Shift+F").</summary>
    public const string BindingKey = "binding";
    /// <summary>Config key for the Hotkey trigger's <see cref="MacroHotkeyMode"/>.</summary>
    public const string ModeKey = "mode";
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
                    ctx.Fire, excluded: true);
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
}
