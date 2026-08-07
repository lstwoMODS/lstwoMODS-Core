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
    public static ConfigEntry<F2Mode> F2ModeEntry;
    public static ConfigEntry<bool> DeveloperModeEntry;

    // COMPATIBILITY (read at overlay creation  changes require a restart to take effect)
    public static ConfigEntry<bool> MainViewportSeparateWindowEntry;
    public static ConfigEntry<bool> DisableMultiViewportEntry;

    // WINDOW BACKGROUND (applies live)
    public static ConfigEntry<ImGuiConfig.WindowBackgroundMode> WindowBackgroundModeEntry;
    public static ConfigEntry<string> WindowBackgroundColorEntry;

    private static Thread _renderThread;

    public static LstwoModsOverlay Window;
    public static UIInspectorWindow UIInspectorWindow;

    private void Awake()
    {
        Instance = this;

        // Everything that dispatches to the main thread (Ref<T>.Changed, element callbacks)
        // needs to know which thread that is, so claim it before any of it can run.
        MainThread.Claim();

        // Keep the Unity player ticking while the game window is unfocused. When the overlay
        // takes keyboard focus the game is technically unfocused, and with the default
        // runInBackground=false Unity stops calling Update() entirely  which freezes the
        // MainThread queue that overlay hotkeys (F2) and UI callbacks are drained on, so they
        // only fire once you alt-tab back. This does not override a game's own timeScale-based
        // pause (Update runs regardless of timeScale); it only prevents the built-in
        // unfocused-freeze that breaks the overlay input path.
        Application.runInBackground = true;

        FontScaleEntry = Config.Bind("UI", "Font Scale", 1f, "Global ImGui font scale.");
        F2ModeEntry = Config.Bind("UI", "F2 Mode", F2Mode.ToggleMenuBarAndPanels,
            "What the F2 hotkey toggles. ToggleMenuBarAndPanels: menu bar + panels together. " +
            "ToggleMenuBarOnly: only the menu bar (panels stay visible, e.g. on a second monitor). " +
            "ToggleNothing: F2 is disabled and everything stays visible.");

        MainViewportSeparateWindowEntry = Config.Bind("Compatibility", "Main Viewport As Separate Window", false,
            "Render the UI in its own standalone window instead of as a transparent overlay on top of the game. " +
            "Useful when the transparent overlay doesn't work (e.g. exclusive fullscreen). Requires a restart.");
        DisableMultiViewportEntry = Config.Bind("Compatibility", "Disable ImGui Multi-Viewport", false,
            "Disable ImGui multi-viewport so panels can't be dragged out into separate OS windows. " +
            "Improves compatibility with some setups. Requires a restart.");

        WindowBackgroundModeEntry = Config.Bind("UI", "Window Background", ImGuiConfig.WindowBackgroundMode.MatchImGui,
            "Background for the UI window. MatchImGui: match the ImGui theme's background color. " +
            "Custom: use 'Window Background Color'. Only visible in 'Main Viewport As Separate Window' mode.");
        WindowBackgroundColorEntry = Config.Bind("UI", "Window Background Color", "#738C99",
            "Custom window background color (hex) used when Window Background = Custom.");

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
                
                if (F2ModeEntry.Value != F2Mode.ToggleMenuBarAndPanels)
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
        MainThread.Drain();

        foreach (var hotkeyManager in UIManager.Windows.Values.Select(x => x.HotkeyManager))
        {
            hotkeyManager.Update();
        }
        
        MainThread.Drain();

        // Isolate each mod's Update: an exception in one must not skip every mod after it in the
        // list (that silently freezes their per-frame logic  e.g. status indicators  while their
        // event/coroutine-driven features keep working, which is near-impossible to diagnose).
        foreach(var mod in Mods)
        {
            try { mod.Update(); }
            catch (Exception e) { LogSource.LogError($"Error updating mod '{mod.GetType().FullName}': {e}"); }
        }

        // Detached (per-context) instances get the same per-frame tick as UI instances.
        foreach (var mod in Hacks.ModRegistry.DetachedInstances)
        {
            try { mod.Update(); }
            catch (Exception e) { LogSource.LogError($"Error updating detached mod '{mod.GetType().FullName}': {e}"); }
        }
        
        MainThread.Drain();
    }

    internal void ToggleUI()
    {
        if (Window?.LstwoModsPanels == null) return;

        // In "toggle nothing" mode F2 is inert  the UI stays as-is.
        if (F2ModeEntry.Value == F2Mode.ToggleNothing) return;

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

    // Saves are write-behind (see DataStorage), so anything changed in the last few seconds is
    // still only in memory at this point. Both hooks are needed: OnApplicationQuit runs on a
    // clean quit, OnDestroy also covers the plugin being torn down without one.
    private void OnApplicationQuit()
    {
        DataStorage.FlushAll();
    }

    private void OnDestroy()
    {
        DataStorage.FlushAll();
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