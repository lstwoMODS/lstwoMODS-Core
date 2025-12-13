
using System;
using System.Collections.Generic;
using ImGuiNET;
using lstwoMODS_Core.Hacks;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;

namespace lstwoMODS_Core.UI.TabMenus
{
    public class ModsTab : BaseTab
    {
        public List<BaseMod> Mods = new();
        
        public ModsTab(string name = "Mods")
        {
            Name = name;
        }

        public void RenderModUI(BaseMod mod)
        {
            ImGui.PushID("MOD " + mod.GetType().FullName);
            var headerOpen = ImGui.CollapsingHeader($"{mod.Name}###HEADER");
            
            if (!string.IsNullOrEmpty(mod.Description) && ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(mod.Description);
            }
            
            if (headerOpen)
            {
                mod.RenderUI();
            }
            
            ImGui.PopID();
        }

        public override void RenderUI()
        {
            foreach (var mod in Mods)
            {
                try
                {
                    RenderModUI(mod);
                } 
                catch (Exception e)
                {
                    Plugin.LogSource.LogError($"Error Rendering Mod UI ({mod.Name}): {e.Message} {e.StackTrace}");
                }
            }
        }

        public override void RefreshUI()
        {
            foreach (var mod in Mods)
            {
                try
                {
                    mod.RefreshUI();
                }
                catch (Exception e)
                {
                    Plugin.LogSource.LogError($"Error Refreshing Mod ({mod.Name}): {e.Message} {e.StackTrace}");
                }
            }
        }
    }
}
