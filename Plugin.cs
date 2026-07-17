using BepInEx;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;
using System;
using System.Diagnostics;
using BepInEx.Logging;
using BepInEx.Configuration;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using lstwoMODS_Core.Hacks;
using lstwoMODS.ImGui.Shared;
using lstwoMODS_Core.Hotkeys;
using lstwoMODS_Core.UI;
using lstwoMODS_Core.UI.TabMenus;
using Debug = UnityEngine.Debug;

namespace lstwoMODS_Core;

[BepInPlugin(GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public const string GUID = "net.lstwo.lstwomods_core";

    // QUICK ACCESS
    public const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
        
    public static ManualLogSource LogSource => Instance.Logger;
    public static ConfigFile ConfigFile => Instance.Config;

    // INSTANCES
    public static Plugin Instance { get; private set; }
    public static List<BaseMod> Mods { get; private set; } = new();

    // UI TOGGLING
    public static Action<bool> OnUIToggle { get; set; }
    public static List<Func<bool>> UIConditions { get; set; } = new();
    
    // CONFIG
    public static ConfigEntry<float> FontScaleEntry;
    public static ConfigEntry<bool> F2MenuBarOnlyEntry;
    public static ConfigEntry<bool> DeveloperModeEntry;

    private static Thread _renderThread;

    public static LstwoModsOverlay Window;
    public static UIInspectorWindow UIInspectorWindow;

    private void Awake()
    {
        Instance = this;

        FontScaleEntry = Config.Bind("UI", "Font Scale", 1f, "Global ImGui font scale.");
        F2MenuBarOnlyEntry = Config.Bind("UI", "F2 Toggles Menubar Only", false, "When enabled, F2 only toggles the menu bar. Panels remain visible so you can move them to a second monitor.");
        
        DeveloperModeEntry = Config.Bind("Developer", "Developer Mode", false, "Enable developer tools and debug windows.");
        
        if (DeveloperModeEntry.Value)
            UIInspectorWindow = new UIInspectorWindow();

        Logger.LogInfo($"Plugin {GUID} is loaded!");
    }

    private void Start()
    {
        UIManager.OnInitialized += () =>
        {
            MainThread.Enqueue(() =>
            {
                InitMods();
                
                Window = new LstwoModsOverlay(GetWindowHandle());
                _ = Window.Initialize();
                
                LstwoModsPanels.StyleEditorWindow.ApplyCurrentPreset();
                
                if (F2MenuBarOnlyEntry.Value)
                {
                    Window.LstwoModsPanels.Enabled = true;
                }
                
                Window.HotkeyManager.Register("lstwomods.toggle-ui", "Toggle UI", KeyCode.F2, HotkeyModifiers.None, ToggleUI);

                Macros.MacroManager.Initialize();
            });
        };
        UIManager.Initialize();
    }

    public static void InitMods()
    {
        InitChildClasses<BaseMod>();
    }

    public static void InitChildClasses<T>()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var types = new List<Type>();

        foreach(var assembly in assemblies)
        {
            try
            {
                types.AddRange(assembly.GetTypes());
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error getting types from assembly '{assembly.FullName}': {ex.Message} {ex.StackTrace}");
            }
        }

        foreach (var type in types)
        {
            try
            {
                if(type.IsSubclassOf(typeof(T)) && !type.IsAbstract)
                {
                    Activator.CreateInstance(type);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error evaluating / initializing type '{type.FullName}': {ex.Message} {ex.StackTrace}");
            }
        }
    }

    public static Coroutine _StartCoroutine(IEnumerator routine)
    {
        return Instance.StartCoroutine(routine);
    }

    private void Update()
    {
        while (MainThread.Queue.TryDequeue(out var action))
        {
            action();
        }

        foreach (var hotkeyManager in UIManager.Windows.Values.Select(x => x.HotkeyManager))
        {
            hotkeyManager.Update();
        }
        
        while (MainThread.Queue.TryDequeue(out var action))
        {
            action();
        }

        foreach(var mod in Mods)
        {
            mod.Update();
        }

        // Detached (per-context) instances get the same per-frame tick as UI instances.
        foreach (var mod in Hacks.ModRegistry.DetachedInstances)
        {
            mod.Update();
        }
        
        while (MainThread.Queue.TryDequeue(out var action))
        {
            action();
        }
    }

    internal void ToggleUI()
    {
        if (Window?.LstwoModsPanels == null) return;
        var panels = Window.LstwoModsPanels;

        if (UIConditions.Any(condition => !condition.Invoke()))
        {
            panels.Enabled = false;
            Window.FocusGameWindow();
            return;
        }

        panels.Enabled = !panels.Enabled;
        if (!panels.Enabled)
            Window.FocusGameWindow();
    }

    private void OnDestroy()
    {
        UIManager.Dispose();
    }

    private delegate bool EnumThreadDelegate(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

    [DllImport("Kernel32.dll")]
    static extern int GetCurrentThreadId();

    static IntPtr GetWindowHandle()
    {
        IntPtr returnHwnd = IntPtr.Zero;
        var threadId = GetCurrentThreadId();
        EnumThreadWindows(threadId,
            (hWnd, lParam) => {
                if(returnHwnd == IntPtr.Zero) returnHwnd = hWnd;
                return true;
            }, IntPtr.Zero);
        return returnHwnd;
    }
}