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
using lstwoMODS_Core.UI;
using lstwoMODS_Core.UI.TabMenus;
using Debug = UnityEngine.Debug;

namespace lstwoMODS_Core;

[BepInPlugin(GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public const string GUID = "net.lstwo.lstwomods_core";

    public static bool DevMode = false;

    // ASSETS
    //public static AssetBundle AssetBundle { get; private set; }
    //public static AssetBundle LstwoModsUImGuiBundle;

    // QUICK ACCESS
    public const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
        
    public static ManualLogSource LogSource => Instance.Logger;
    public static ConfigFile ConfigFile => Instance.Config;

    // INSTANCES
    public static Plugin Instance { get; private set; }
    public static AssetUtils AssetUtils { get; set; }
    
    //public static MainPanel MainPanel { get; private set; }
    //public static KeybindPanel KeybindPanel { get; private set; }

    public static List<BaseTab> TabMenus { get; private set; } = new();
    public static List<BaseMod> Mods { get; private set; } = new();

    //public static SettingsTab SettingsTab;

    //public static ProfilesTab ProfilesTab { get; private set; }

    // OTHER FEATURES
    //public static KeybindManager KeybindManager { get; private set; }

    // UI TOGGLING
    public static Action<bool> OnUIToggle { get; set; }
    public static List<Func<bool>> UIConditions { get; set; } = new();
    
    // CONFIG
    //public static ConfigEntry<float> UIScaleFactor;

    private static Thread _renderThread;

    private static LstwoModsOverlay _window;

    private void Awake()
    {
        Instance = this;

        //UIScaleFactor = Config.Bind("UI", "Scale Factor", 1f, "Works but may lead to unwanted side effects. Here for accessibility reasons.");
            
        /*AssetUtils = new();
        AssetUtils.AssetBundles = new()
        {
            new("lstwoMODS_Core.Resources.assets.6000.bundle", new("6000.0.23")),
            new("lstwoMODS_Core.Resources.assets.2020.bundle", new("2020.3.28")),
            new("lstwoMODS_Core.Resources.assets.2017.bundle", new("2017.1.0")),
            new("lstwoMODS_Core.Resources.assets.5.6.bundle", new("5.6.0")),
            new("lstwoMODS_Core.Resources.assets.5.3.4.bundle", new("5.3.4")),
            new("lstwoMODS_Core.Resources.assets.5.2.5.bundle", new("5.2.5")),
        };

        AssetBundle = AssetUtils.LoadCompatibleAssetBundle(GetType().Assembly);*/
        //KeybindManager = gameObject.AddComponent<KeybindManager>();
        
        //HacksUIHelper.LoadConfig();
        //KeybindManager.LoadAllKeybinds();

        //var assetBundles = Path.GetDirectoryName(Assembly.GetAssembly(typeof(ImGui)).Location) + "/assets/";
        //LstwoModsUImGuiBundle = UnityEngine.AssetBundle.LoadFromFile(assetBundles + "lstwomods_uimgui");
        
        //SettingsTab = new();
        
        Logger.LogInfo($"Plugin {GUID} is loaded!");
    }

    private void Start()
    {
        UIManager.OnInitialized += async () =>
        {
            _window = new LstwoModsOverlay(GetWindowHandle());
            await _window.Initialize();
            InitMods();
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
        
        if(Input.GetKeyDown(KeyCode.F2))
        {
            ToggleUI();
        }
        
        while (MainThread.Queue.TryDequeue(out var action))
        {
            action();
        }

        foreach(var mod in Mods)
        {
            mod.Update();
        }
        
        while (MainThread.Queue.TryDequeue(out var action))
        {
            action();
        }
    }

    private void ToggleUI()
    {
        if (UIConditions.Any(condition => !condition.Invoke()))
        {
            //LstwoModsUI.Enabled = false;
            return;
        }

        //LstwoModsUI.Enabled = !LstwoModsUI.Enabled;
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