using System;
using System.Collections.Generic;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS_Core.UI.Elements;

namespace lstwoMODS_Core.UI.TabMenus;

public class SettingsWindow : BaseWindow
{
    private readonly Ref<float> _fontScaleRef = new(1f);
    private readonly Ref<int> _f2ModeRef = new(0);
    private readonly Ref<bool> _mainViewportSeparateRef = new(false);
    private readonly Ref<bool> _disableMultiViewportRef = new(false);
    private readonly Ref<int> _bgModeRef = new(0);
    private readonly Ref<Color> _bgColorRef = new(Color.gray);

    private static readonly string[] _f2ModeItems =
    {
        "Toggle Menu Bar + Panels",
        "Toggle Menu Bar Only (Panels Always Visible)",
        "Toggle Nothing (Always Visible)",
    };

    private static readonly string[] _bgModeItems =
    {
        "Custom Color",
        "Match ImGui Background",
    };

    /// <summary>Push the current window-background config to the live overlay (applies immediately).</summary>
    private static void ApplyBackgroundToWindow()
    {
        if (Plugin.Window == null) return;

        var cfg = Plugin.Window.Config;
        cfg.BackgroundMode        = Plugin.WindowBackgroundModeEntry.Value;
        cfg.WindowBackgroundColor = LstwoModsOverlay.ParseBackgroundColor(Plugin.WindowBackgroundColorEntry.Value);
        Plugin.Window.Config = cfg;
    }

    /// <summary>
    /// Subscribe to append custom elements to the Settings window.
    /// The list is passed before the Group is constructed, so additions are fully registered.
    /// </summary>
    public static event Action<List<BaseUIElement>> OnBuildUI;

    public SettingsWindow()
    {
        Name = "Settings";
        TitleIcon = Lucide.Settings;
    }

    public override Group ConstructUI()
    {
        var resetStateDialog = new ConfirmDialog("ResetWindowLayout ConfirmDialog", message:   "Reset UI State to default?", onConfirm: () => {
            Plugin.Window.LoadIniSettings(LstwoModsPanels.GenerateDefaultIni());
        }).WithId("SettingsWindow.resetStateDialog") as ConfirmDialog;

        var resetModOrderDialog = new ConfirmDialog("ResetModOrder ConfirmDialog", message:   "Reset the mod list order to default?", onConfirm: ModsWindow.ResetAllOrders).WithId("SettingsWindow.resetModOrderDialog") as ConfirmDialog;

        var elements = new List<BaseUIElement>
        {
            new DragFloat("Font Scale###FontScale", 1f, 0.01f, 0.5f, 3f, onValueChanged: v =>
            {
                Plugin.FontScaleEntry.Value = v;
                if (Plugin.Window == null) return;
                
                var cfg = Plugin.Window.Config;
                cfg.FontGlobalScale = v;
                Plugin.Window.Config = cfg;
                
            }).WithValue(_fontScaleRef),
            
            new Combo("F2 Mode###F2Mode", _f2ModeItems,
                onChanged: v =>
                {
                    Plugin.F2ModeEntry.Value = (F2Mode)v;
                    if (Plugin.Window?.LstwoModsPanels == null) return;

                    var panels = Plugin.Window.LstwoModsPanels;
                    // Show everything when leaving the default toggle mode; otherwise just re-run
                    // the visibility logic against the current toggle state.
                    if ((F2Mode)v != F2Mode.ToggleMenuBarAndPanels)
                        panels.Enabled = true;
                    else
                        panels.Refresh();

                }).WithSelectedIndex(_f2ModeRef),

            new Checkbox("Compatibility: Main Viewport As Separate Window (needs restart)###MainViewportSeparate",
                onChanged: v => Plugin.MainViewportSeparateWindowEntry.Value = v)
                .WithValue(_mainViewportSeparateRef),

            new Checkbox("Compatibility: Disable ImGui Multi-Viewport (needs restart)###DisableMultiViewport",
                onChanged: v => Plugin.DisableMultiViewportEntry.Value = v)
                .WithValue(_disableMultiViewportRef),

            new Combo("Window Background###WindowBgMode", _bgModeItems,
                onChanged: v =>
                {
                    Plugin.WindowBackgroundModeEntry.Value = (ImGuiConfig.WindowBackgroundMode)v;
                    ApplyBackgroundToWindow();
                }).WithSelectedIndex(_bgModeRef),

            new ColorEdit4("Window Background Color###WindowBgColor", Color.gray,
                onChanged: c =>
                {
                    Plugin.WindowBackgroundColorEntry.Value = "#" + ColorUtility.ToHtmlStringRGB(c);
                    ApplyBackgroundToWindow();
                }, flags: ImGuiColorEditFlags.NoAlpha).WithValue(_bgColorRef),

            resetStateDialog,
            new Button("Reset Saved UI State (Window Layout, etc.)", resetStateDialog.Show).WithContentWidth(),

            resetModOrderDialog,
            new Button("Reset Mod Order", resetModOrderDialog.Show).WithContentWidth()
        };

        OnBuildUI?.Invoke(elements);

        return new Group("Settings", elements.ToArray());
    }

    public override void RefreshUI()
    {
        _fontScaleRef.Value = Plugin.FontScaleEntry.Value;
        _f2ModeRef.Value = (int)Plugin.F2ModeEntry.Value;
        _mainViewportSeparateRef.Value = Plugin.MainViewportSeparateWindowEntry.Value;
        _disableMultiViewportRef.Value = Plugin.DisableMultiViewportEntry.Value;
        _bgModeRef.Value = (int)Plugin.WindowBackgroundModeEntry.Value;
        if (ColorUtility.TryParseHtmlString(Plugin.WindowBackgroundColorEntry.Value, out var bg))
            _bgColorRef.Value = bg;
    }
}
