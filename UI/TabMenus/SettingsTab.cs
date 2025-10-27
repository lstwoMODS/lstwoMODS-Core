using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;

namespace lstwoMODS_Core.UI.TabMenus
{
    public class SettingsTab : BaseTab
    {
        private int selectedTabMode;
        private string[] tabModes;
        
        public SettingsTab()
        {
            Name = "Settings";
        }

        public override void RenderUI()
        {
            if (ImGui.Combo("Tab Mode", ref selectedTabMode, tabModes, tabModes.Length))
            {
                Window.tabMode = (Window.TabMode)selectedTabMode;
                Window.tabModeConfigEntry.Value = selectedTabMode;
            }
        }

        public override void RefreshUI()
        {
            tabModes = Enum.GetNames(typeof(Window.TabMode));
        }
    }
}
