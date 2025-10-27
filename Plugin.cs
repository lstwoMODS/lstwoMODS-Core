using BepInEx;
using UnityEngine;
using lstwoMODS_Core.UI;
using System.Collections.Generic;
using lstwoMODS_Core.UI.TabMenus;
using System.Reflection;
using System.Collections;
using System;
using BepInEx.Logging;
using BepInEx.Configuration;
using System.Linq;
using System.IO;
using ImGuiNET;
using lstwoMODS_Core.Hacks;
using UImGui.Assets;

namespace lstwoMODS_Core;

[BepInPlugin(GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public const string GUID = "net.lstwo.lstwomods_core";

    public static bool DevMode = false;

    // ASSETS
    public static AssetBundle AssetBundle { get; private set; }
    //public static AssetBundle LstwoModsUImGuiBundle;
    public static AssetBundle UImGuiBundle;

    // QUICK ACCESS
    public const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
        
    public static ManualLogSource LogSource => Instance.Logger;
    public static ConfigFile ConfigFile => Instance.Config;

    // INSTANCES
    public static Plugin Instance { get; private set; }
    public static AssetUtils AssetUtils { get; set; }

    public static UImGui.UImGui ImGuiRenderer;
    //public static MainPanel MainPanel { get; private set; }
    //public static KeybindPanel KeybindPanel { get; private set; }

    public static List<BaseTab> TabMenus { get; private set; } = new();
    public static List<BaseMod> Mods { get; private set; } = new();

    public static SettingsTab SettingsTab;

    //public static ProfilesTab ProfilesTab { get; private set; }

    // OTHER FEATURES
    //public static KeybindManager KeybindManager { get; private set; }

    // UI TOGGLING
    public static Action<bool> OnUIToggle { get; set; }
    public static List<Func<bool>> UIConditions { get; set; } = new();
        
    // CONFIG
    public static ConfigEntry<float> UIScaleFactor;

    public static Action OnUIInitialize;

    private static FieldInfo uImGuiCameraField;
        

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

        var assetBundles = Path.GetDirectoryName(Assembly.GetAssembly(typeof(ImGui)).Location) + "/assets/";
        UImGuiBundle = UnityEngine.AssetBundle.LoadFromFile(assetBundles + "uimgui");
        //LstwoModsUImGuiBundle = UnityEngine.AssetBundle.LoadFromFile(assetBundles + "lstwomods_uimgui");

        OnUIInitialize += Window.Initialize;

        LoadStyle();

        SettingsTab = new();

        Logger.LogInfo($"Plugin {GUID} is loaded!");
    }

    private static void LoadStyle()
    {
        var folderPath = @$"{AppDomain.CurrentDomain.BaseDirectory}\lstwoMODS\style";
        var styleAsset = UImGuiBundle.LoadAsset<StyleAsset>("lstwoMODS uImGui Style");

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var styleFilePath = $@"{folderPath}\style.json";

        if (File.Exists(styleFilePath))
        {
            StyleManager.LoadFromJson(styleAsset, styleFilePath);
        }

        var templateStylePath = $@"{folderPath}\template.json";
        
        StyleManager.SaveToJson(styleAsset, templateStylePath);
        
        File.WriteAllText($@"{folderPath}\README.txt", 
            "The template.json contains the default style parameters from lstwoMODS " +
            "and will get automatically updated with each launch. " +
            "Duplicate the file to change the parameters. " +
            "The mod will look for a style.json file in this folder on every launch.");
    }

    private void Start()
    {
        InitMods();
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
        if(Input.GetKeyDown(KeyCode.F2))
        {
            ToggleUI();
        }

        foreach(var mod in Mods)
        {
            mod.Update();
        }
    }

    private void ToggleUI()
    {
        if (UIConditions.Any(condition => !condition.Invoke()))
        {
            Window.Enabled = false;
            return;
        }

        Window.Enabled = !Window.Enabled;
    }
}