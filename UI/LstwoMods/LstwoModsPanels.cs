using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using lstwoMODS_Core.UI.Elements;
using lstwoMODS_Core.UI.TabMenus;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;
using UnityEngine;

namespace lstwoMODS_Core.UI;

public class LstwoModsPanels : UIComponent
{
    //public static ConfigEntry<int> TabModeConfigEntry;

    public static readonly string ModName = DateTime.Now.Month == 4 && DateTime.Now.Day == 1 ? "ProjectMods Enterprise" : "lstwoMODS";

    public static SettingsWindow SettingsWindow;
    public static StyleEditorWindow StyleEditorWindow;
    public static MacrosWindow MacrosWindow;

    public static readonly List<BaseWindow> Windows;
    
    public string Font = "inter";
    
    public bool Enabled
    {
        get => enabled;
        set
        {
            var changed = enabled != value;
            enabled = value;
            if (changed)
                Plugin.OnUIToggle?.Invoke(enabled);
            Refresh();
        }
    }

    private bool enabled;
    private bool menuBarEnabled;
    private Container windowsModeUI;
    private Container mainMenuBarUI;

    private static LstwoModsPanels _instance;

    private readonly List<(BaseWindow Tab, MenuItem MenuItem)> _windowMenuItems = new();
    private readonly Dictionary<BaseWindow, GuiWindow> _windowsByTab = new();

    static LstwoModsPanels()
    {
        Windows = [];
        StyleEditorWindow = new StyleEditorWindow();
        SettingsWindow = new SettingsWindow();
        MacrosWindow = new MacrosWindow();
    }

    public LstwoModsPanels() : base("lstwoMODS Panels")
    {
        _instance = this;

        windowsModeUI = WindowsModeUI();
        mainMenuBarUI = MainMenuBarUI();

        windowsModeUI.SetVisible(false);
        mainMenuBarUI.SetVisible(false);

        Add(windowsModeUI, mainMenuBarUI, ModContextMenuService.BuildUI());
        Refresh();
    }

    /// <summary>Open (and focus) the window that hosts <paramref name="window"/>. Used to send the
    /// user to a tab, e.g. the Macros window after creating a macro from a context menu.</summary>
    public static void RevealWindow(BaseWindow window)
    {
        if (_instance == null || window == null) return;
        if (!_instance._windowsByTab.TryGetValue(window, out var gui)) return;

        ((WindowData)gui.Data).Open = true;
        gui.MarkChanged();
        gui.FocusNextFrame();
    }
    
    public const uint WindowsModeDockSpaceId = 0x4C535457;

    /// <summary>DataStorage id for persisted per-window open/closed state (data.json bag).</summary>
    private const string WindowStateStorageId = "WindowState";

    private Container WindowsModeUI()
    {
        _windowMenuItems.Clear();
        _windowsByTab.Clear();

        var windowElements = Windows.Select(BaseUIElement (tab) =>
        {
            try
            {
                var key = tab.GetType().FullName;

                var wasOpen = !DataStorage.BagEntryExists(WindowStateStorageId, key) || DataStorage.LoadFromBag<bool>(WindowStateStorageId, key);

                var menuItem = new MenuItem(tab.Name).WithSelected(wasOpen);

                var window = new GuiWindow(key, tab.WindowTitle, tab.ConstructUI())
                    .WithId(key)
                    .WithSize(853, 480, ImGuiCond.FirstUseEver)
                    .WithDock(WindowsModeDockSpaceId)
                    .WithOpen(wasOpen);

                window.OnOpen(open =>
                {
                    DataStorage.SaveToBag(WindowStateStorageId, key, open);
                    menuItem.Selected = open;
                    if (open) tab.RefreshUI();
                });

                window.OnFocus(() => tab.RefreshUI());

                menuItem.OnClick(() =>
                {
                    var open = menuItem.Selected;
                    ((WindowData)window.Data).Open = open;
                    window.MarkChanged();

                    DataStorage.SaveToBag(WindowStateStorageId, key, open);
                });

                _windowMenuItems.Add((tab, menuItem));
                _windowsByTab[tab] = window;
                return (BaseUIElement)window;
            }
            catch (Exception e)
            {
                Plugin.LogSource.LogError(
                    $"Error Constructing Window UI for \"{tab.GetType().FullName}\": {e.Message} {e.StackTrace}");
                return null;
            }
        }).Where(x => x != null).ToArray();

        return new Container("WindowsModeUI", windowElements);
    }

    private Container MainMenuBarUI()
    {
        return new Container("MainMenuBarUI",
            new MainMenuBar("lstwoMODS_MainMenuBar", [
                new UIText("modname", ModName),
                new Separator("sep"),
                
                .._windowMenuItems.Select(pair => (BaseUIElement)pair.MenuItem).ToArray()
            ])
        );
    }

    public void Refresh()
    {
        var mode = Plugin.F2ModeEntry?.Value ?? F2Mode.ToggleMenuBarAndPanels;

        var windowsVisible = mode != F2Mode.ToggleMenuBarAndPanels || Enabled;
        var menuBarVisible = mode == F2Mode.ToggleNothing || Enabled;

        windowsModeUI.SetVisible(windowsVisible);
        mainMenuBarUI.SetVisible(menuBarVisible);
        MarkChanged();
    }

    /// <summary>
    /// Subscribe to append your own window entries to the default ini on layout reset.
    /// Write one or more <c>[Window][Title]\nDockId=0x...,index\n</c> blocks into the StringBuilder.
    /// </summary>
    public static event Action<StringBuilder> OnGenerateDefaultIni;

    /// <summary>
    /// Generates an ini string that restores the default layout for the current tab mode.
    /// Load this via <c>Window.LoadIniSettings()</c> when resetting layout, instead of an
    /// empty string. Clearing the ini mid-session doesn't re-trigger FirstUseEver.
    /// </summary>
    public static string GenerateDefaultIni()
    {
        var sb = new StringBuilder();

        for (var i = 0; i < Windows.Count; i++)
        {
            sb.AppendLine($"[Window][###{Windows[i].Name}]");
            sb.AppendLine($"DockId=0x{WindowsModeDockSpaceId:X8},{i}");
            sb.AppendLine();
        }

        sb.AppendLine("[Docking][Data]");
        sb.AppendLine($"DockNode          ID=0x{WindowsModeDockSpaceId:X8} Pos=0,0 Size=853,480");

        OnGenerateDefaultIni?.Invoke(sb);

        return sb.ToString();
    }
}