using System;
using System.Collections.Generic;
using lstwoMODS_Core.UI.Elements;

namespace lstwoMODS_Core.UI.TabMenus;

public class SettingsWindow : BaseWindow
{
    private readonly Ref<float> _fontScaleRef = new(1f);
    private readonly Ref<bool> _f2MenuBarOnlyRef = new(false);

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
            
            new Checkbox("F2 Only Toggles Menubar (Panels Always Visible)###F2MenuBarOnly",
                onChanged: v =>
                {
                    Plugin.F2MenuBarOnlyEntry.Value = v;
                    if (Plugin.Window?.LstwoModsPanels == null) return;
                    
                    var panels = Plugin.Window.LstwoModsPanels;
                    panels.Enabled = true;
                    
                }).WithValue(_f2MenuBarOnlyRef),
            
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
        _f2MenuBarOnlyRef.Value = Plugin.F2MenuBarOnlyEntry.Value;
    }
}
