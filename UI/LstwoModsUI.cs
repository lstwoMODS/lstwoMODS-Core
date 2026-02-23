using System;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using BepInEx.Configuration;
using Hexa.NET.ImGui;
using UnityEngine;

namespace lstwoMODS_Core.UI;

public static class LstwoModsUI
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
    
    private static bool hasInitialized = false;

    public static ImFontPtr Font;
    public static TabMode tabMode;

    internal static ConfigEntry<int> tabModeConfigEntry;

    internal static void Initialize()
    {
        tabModeConfigEntry = Plugin.ConfigFile.Bind("General", "TabMode", (int)TabMode.Tabs, "");
        tabMode = (TabMode)tabModeConfigEntry.Value;
    }

    internal static void LoadFont(ImFontAtlasPtr fontAtlas)
    {
        unsafe
        {
            var io = ImGui.GetIO();
            Font = io.Fonts.AddFontFromFileTTF($@"{Application.streamingAssetsPath}\mods\net.lstwo.lstwoMODS\InterVariable.ttf", 18, null, io.Fonts.GetGlyphRangesDefault());
        }
    }

    internal static void Render()
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
        
        ImGui.SetNextWindowSize(new Vec2(1280, 720), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new Vec2(Screen.width / 2 - 640, Screen.height / 2 - 360), ImGuiCond.FirstUseEver);

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
    
    public static void LoadStyle(ImGuiStylePtr style)
    {
        style.WindowMinSize = new Vec2(10, 10);
        style.Alpha = 1.0f;
        style.WindowPadding = new(8, 8);
        style.WindowRounding = 8;
        style.WindowBorderSize = 1;
        style.ChildRounding = 6;
        style.ChildBorderSize = 1;
        style.PopupRounding = 8;
        style.PopupBorderSize = 1;
        style.FramePadding = new(4, 3);
        style.FrameRounding = 6;
        style.FrameBorderSize = 0;
        style.ItemSpacing = new(8, 4);
        style.ItemInnerSpacing = new(4, 4);
        style.CellPadding = new(0, 0);
        style.TouchExtraPadding = new(0, 0);
        style.ScrollbarRounding = 9;
        style.TabRounding = 6;
        style.TabBorderSize = 0;
        style.DisplayWindowPadding = new(19, 19);
        style.AntiAliasedLines = true;
        style.AntiAliasedFill = true;
        style.AntiAliasedLinesUseTex = true;
        style.CurveTessellationTol = 1.12f;
        style.CircleTessellationMaxError = 0.3f;
        
        style.Colors[0] = new(1, 1, 1, 1);
        style.Colors[2] = new(0.295f, 0.308f, 0.333f, 1);
        style.Colors[3] = new(0.09411765f, 0.109803922f, 0.13333334f, 1);
        style.Colors[4] = new(0.0784313753f, 0.0784313753f, 0.0784313753f, 1);
        style.Colors[5] = new(0.431372583f, 0.431372583f, 0.5019608f, 0.545098066f);
        style.Colors[21] = new(0.258823544f, 0.5882353f, 0.9803922f, 0.3529412f);
        style.Colors[22] = new(0.258823544f, 0.5882353f, 0.9803922f, 1.0f);
        style.Colors[23] = new(0.0588235334f, 0.5294118f, 0.9803922f, 1.0f);
        
        var folderPath = @$"{AppDomain.CurrentDomain.BaseDirectory}\lstwoMODS\style";

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        
        var templateStylePath = $@"{folderPath}\template.json";
        StyleManager.SaveToJson(templateStylePath, style);

        var styleFilePath = $@"{folderPath}\style.json";
        if (File.Exists(styleFilePath))
        {
            StyleManager.LoadFromJson(styleFilePath);
        }
        
        File.WriteAllText($@"{folderPath}\README.txt", 
            "The template.json contains the default style parameters from lstwoMODS " +
            "and will get automatically updated with each launch. " +
            "Duplicate the file to change the parameters. " +
            "The mod will look for a style.json file in this folder on every launch.");
    }

    public enum TabMode
    {
        Tabs,
        Windows
    }
}