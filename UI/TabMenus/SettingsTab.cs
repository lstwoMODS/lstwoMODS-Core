using BepInEx.Configuration;
using System;
using Hexa.NET.ImGui;

namespace lstwoMODS_Core.UI.TabMenus;

public class SettingsTab : BaseTab
{
    private static readonly string _confirmDialogId = typeof(SettingsTab).FullName + "_ResetWindowLayout";

    private int _selectedTabMode;
    private string[] _tabModes;
    private bool _confirmDialogOpen;
        
    public SettingsTab()
    {
        Name = "Settings";
    }

    public override void RenderUI()
    {
        if (ImGui.Combo("Tab Mode", ref _selectedTabMode, _tabModes, _tabModes.Length))
        {
            LstwoModsUI.tabMode = (LstwoModsUI.TabMode)_selectedTabMode;
            LstwoModsUI.tabModeConfigEntry.Value = _selectedTabMode;
        }

        if (ImGui.Button("Reset Saved UI State (Window Layout, etc.)"))
        {
            ImGui.OpenPopup(_confirmDialogId);
            _confirmDialogOpen = true;
        }

        if (ModsUIHelper.ConfirmDialog(_confirmDialogId, "Reset ui state to default?", ref _confirmDialogOpen))
        {
            ImGui.LoadIniSettingsFromMemory("");
        }
    }

    public override void RefreshUI()
    {
        _tabModes = Enum.GetNames(typeof(LstwoModsUI.TabMode));
        _selectedTabMode = LstwoModsUI.tabModeConfigEntry.Value;
    }
}