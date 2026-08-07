using System.Collections.Generic;
using lstwoMODS_Core.Hacks;
using lstwoMODS_Core.Macros;
using lstwoMODS_Core.UI.Elements;

namespace lstwoMODS_Core.UI;

/// <summary>
/// Public helpers for the right-click "Add to macro" / "Create hotkey" context menu.
///
/// The auto UI builder wraps every macroable setting/action with <see cref="ForSetting"/> /
/// <see cref="ForAction"/> automatically. Mods that build a custom panel can:
///  * call <see cref="ForSetting"/> / <see cref="ForAction"/> to wrap their own widget, passing
///    any <c>extraItems</c> they want appended (nothing is taken away  add your own freely); or
///  * use <see cref="Items"/> to drop just the two entries into a hand-rolled
///    <see cref="ContextMenu"/> anywhere among their own menu items.
/// </summary>
public static class ModContextMenu
{
    /// <summary>The two menu items ("Add to macro", "Create hotkey") for a resolved
    /// <see cref="MacroRegistry"/> method id. <paramref name="boolSetting"/> makes the hotkey's
    /// toggle option flip a bool value each press.</summary>
    public static IEnumerable<BaseUIElement> Items(string methodId, string defaultName, bool boolSetting = false)
    {
        var safe = Sanitize(methodId);
        yield return new MenuItem($"{Lucide.Workflow} Add to macro##ctx-macro-{safe}",
            () => ModContextMenuService.OpenCreateMacro(methodId, defaultName));
        yield return new MenuItem($"{Lucide.Keyboard} Create hotkey##ctx-hotkey-{safe}",
            () => ModContextMenuService.OpenCreateHotkey(methodId, defaultName, boolSetting));
    }

    /// <summary>Wrap <paramref name="trigger"/> so right-clicking it offers "Add to macro" /
    /// "Create hotkey" for <paramref name="action"/>, plus any <paramref name="extraItems"/>.</summary>
    public static ContextMenu ForAction(BaseUIElement trigger, ModActionDescriptor action, params BaseUIElement[] extraItems)
        => Build($"ctx-{action.InvokeTarget.GetType().FullName}.{action.MethodName}",
            trigger, MacroManager.MethodIdFor(action), $"{action.ModName} {action.Label}",
            boolSetting: false, extraItems);

    /// <summary>Wrap <paramref name="trigger"/> so right-clicking it offers "Add to macro" /
    /// "Create hotkey" for <paramref name="setting"/>, plus any <paramref name="extraItems"/>.</summary>
    public static ContextMenu ForSetting(BaseUIElement trigger, ModSettingDescriptor setting, params BaseUIElement[] extraItems)
        => Build($"ctx-{setting.ValueTarget.GetType().FullName}.{setting.MemberName}",
            trigger, MacroManager.MethodIdFor(setting), $"{setting.ModName} {setting.Label}",
            boolSetting: setting.ValueType == typeof(bool), extraItems);

    private static ContextMenu Build(string name, BaseUIElement trigger, string methodId, string defaultName,
        bool boolSetting, BaseUIElement[] extraItems)
    {
        var items = new List<BaseUIElement>(Items(methodId, defaultName, boolSetting));
        if (extraItems is { Length: > 0 })
            items.AddRange(extraItems);
        return new ContextMenu(name, trigger, items.ToArray());
    }

    private static string Sanitize(string id) => id?.Replace('#', '_') ?? "";
}
