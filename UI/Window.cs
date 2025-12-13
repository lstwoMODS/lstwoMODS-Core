using System;
using System.Drawing.Printing;
using System.Linq;
using BepInEx.Configuration;
using ImGuiNET;
using UnityEngine;

namespace lstwoMODS_Core.UI;

public static class Window
{
    public static bool Enabled
    {
        get => enabled;
        set
        {
            Plugin.LogSource.LogMessage("Window Toggled: " + value);

            enabled = value;
            Plugin.OnUIToggle(value);

            if (value)
            {
                Refresh();
            }
        }
    }

    internal static bool enabled;

    public static ImFontPtr Font;
    public static TabMode tabMode;

    internal static ConfigEntry<int> tabModeConfigEntry;

    internal static void Initialize()
    {
        tabModeConfigEntry = Plugin.ConfigFile.Bind("General", "TabMode", (int)TabMode.Tabs, "");
        tabMode = (TabMode)tabModeConfigEntry.Value;

        Plugin.ImGuiRenderer.Layout += Render;
        Plugin.OnUIToggle(false);
    }

    private static void Render(UImGui.UImGui uImGui)
    {
        if (!Enabled)
        {
            return;
        }

        if (tabMode == TabMode.Tabs)
        {
            Render_TabMode();
        }
        else if (tabMode == TabMode.Windows)
        {
            Render_WindowsMode();
        }
    }

    private static void Render_TabMode()
    {
        var windowTitle = DateTime.Today.Month == 4 && DateTime.Today.Day == 1 ? "Azzamods" : "lstwoMODS";
        
        ImGui.SetNextWindowSize(new Vector2(1280, 720), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new Vector2(Screen.width / 2 - 640, Screen.height / 2 - 360), ImGuiCond.FirstUseEver);

        var oldEnabledValue = enabled;

        if (ImGui.Begin(windowTitle, ref enabled, ImGuiWindowFlags.NoCollapse))
        {
            if (ImGui.BeginTabBar("lstwoMODS_MainTabs"))
            {
                foreach (var tab in Plugin.TabMenus)
                {
                    if (!ImGui.BeginTabItem(tab.Name))
                    {
                        continue;
                    }
                    
                    ImGui.PushID("TAB " + tab.GetType().FullName);
                    tab.RenderUI();
                    ImGui.PopID();
                    
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            ImGui.End();
        }

        if (oldEnabledValue != enabled)
        {
            Enabled = enabled;
        }
    }
    
    private static void Render_WindowsMode()
    {
        var i = 0;
        
        foreach (var tab in Plugin.TabMenus)
        { 
            if (ImGui.Begin(tab.Name + "###" + tab.GetType().FullName + "_" + tab.Name + "_" + i, ImGuiWindowFlags.None))
            {
                tab.RenderUI();
            }
            
            ImGui.End();
            i++;
        }
    }

    public static void Refresh()
    {
        foreach (var tab in Plugin.TabMenus)
        {
            tab.RefreshUI();
        }
    }

    public enum TabMode
    {
        Tabs,
        Windows
    }
}