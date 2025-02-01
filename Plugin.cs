using BepInEx;
using UnityEngine;
using UniverseLib.UI;
using UniverseLib;
using UniverseLib.Config;
using lstwoMODS_Core.UI;
using System.Collections.Generic;
using lstwoMODS_Core.UI.TabMenus;
using lstwoMODS_Core.Hacks;
using System.Reflection;
using System.Collections;
using System;
using BepInEx.Logging;
using lstwoMODS_Core.UI.Keybinds;
using lstwoMODS_Core.Keybinds;
using BepInEx.Configuration;
using System.Linq;
using System.IO;

namespace lstwoMODS_Core
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        // ASSETS
        public static AssetBundle AssetBundle { get; private set; }

        // QUICK ACCESS
        public const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        
        public static ManualLogSource LogSource { get => Instance.Logger; }
        public static ConfigFile ConfigFile { get => Instance.Config; }

        // INSTANCES
        public static Plugin Instance { get; private set; }

        public static UIBase UiBase { get; private set; }
        public static MainPanel MainPanel { get; private set; }
        public static KeybindPanel KeybindPanel { get; private set; }

        public static List<BaseTab> TabMenus { get; private set; } = new();
        public static List<BaseHack> Hacks { get; private set; } = new();

        // OTHER FEATURES
        public static KeybindManager KeybindManager { get; private set; }

        // UI TOGGLING
        public static Action<bool> OnUIToggle { get; private set; }
        public static List<Func<bool>> UIConditions { get; private set; } = new();

        private void Awake()
        {
            Instance = this;
            AssetBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "lstwo.lstwomods.assets"));
            KeybindManager = gameObject.AddComponent<KeybindManager>();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void Start()
        {
            InitMods();
            InitUI();
        }

        public static void InitMods()
        {
            InitChildClasses<BaseHack>();
        }

        public static void InitUI()
        {
            UniverseLibConfig config = new()
            {
                Disable_EventSystem_Override = false,
                Force_Unlock_Mouse = true
            };

            Universe.Init(1f, () =>
            {
                UiBase = UniversalUI.RegisterUI("lstwo.NotAzza", null);

                MainPanel = new(UiBase);
                KeybindPanel = new(UiBase);

                KeybindPanel.Enabled = false;
                UiBase.Enabled = false;
            }, null, config);
        }

        public static void InitChildClasses<T>()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var childTypes = assemblies.SelectMany(assembly => assembly.GetTypes()).Where(t => t.IsSubclassOf(typeof(T)) && !t.IsAbstract);

            foreach (var type in childTypes)
            {
                Activator.CreateInstance(type);
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

            foreach(var hack in Hacks)
            {
                hack.Update();
            }
        }

        private void ToggleUI()
        {
            foreach (var condition in UIConditions)
            {
                if (!condition.Invoke())
                {
                    UiBase.Enabled = false;
                    return;
                }
            }

            var enabled = !UiBase.Enabled;
            UiBase.Enabled = enabled;

            if (enabled)
            {
                MainPanel.Refresh();
            }

            OnUIToggle(enabled);
        }
    }
}