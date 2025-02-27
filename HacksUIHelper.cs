using BepInEx.Configuration;
using lstwoMODS_Core.Keybinds;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UniverseLib.UI;
using UniverseLib.UI.Models;

namespace lstwoMODS_Core
{
    public class HacksUIHelper
    {
        public static Sprite RoundedRect { get; private set; }

        public static Font Font => interFont;

        private static Font interFont;

        private static ConfigEntry<string> configTheme;
        private static ConfigEntry<string> configButtonColor;
        private static ConfigEntry<string> configButtonColor2;
        private static ConfigEntry<string> configButtonColor4;
        private static ConfigEntry<string> configBGColor1;
        private static ConfigEntry<string> configBGColor2;
        private static ConfigEntry<string> configTabMenuBG;
        private static ConfigEntry<string> configHacksMenuBG;

        private static Dictionary<string, ColorTheme> themes = new();

        public GameObject root { get; private set; }

        static HacksUIHelper()
        {
            RoundedRect ??= Plugin.AssetBundle.LoadAsset<Sprite>("RoundedRect");
            interFont ??= Plugin.AssetBundle.LoadAsset<Font>("Inter");
        }

        public static void LoadConfig()
        {
            themes.Add("original", originalTheme);


            configTheme = Plugin.ConfigFile.Bind("Theme", "Theme", "original", "The theme to use for lstwoMODS.\nAvailable themes: original.\nUse custom for custom colors defined in Theme.Custom category.");

            configButtonColor = Plugin.ConfigFile.Bind("Theme.Custom", "Button Color 1", "#38404F", "Custom hex color for regular buttons.");
            configButtonColor2 = Plugin.ConfigFile.Bind("Theme.Custom", "Button Color 2", "#101821", "Custom hex color for tab buttons.");
            configButtonColor4 = Plugin.ConfigFile.Bind("Theme.Custom", "Button Color 3", "#26618c", "Custom hex color for special buttons.");

            configBGColor1 = Plugin.ConfigFile.Bind("Theme.Custom", "BG Color 1", "#21252d", "Custom hex color for mod background color 1.");
            configBGColor2 = Plugin.ConfigFile.Bind("Theme.Custom", "BG Color 2", "#1d2129", "Custom hex color for mod background color 2.");

            configTabMenuBG = Plugin.ConfigFile.Bind("Theme.Custom", "Tab Menu BG Color", "#1a2837", "Custom hex color for tab menu background.");
            configHacksMenuBG = Plugin.ConfigFile.Bind("Theme.Custom", "Mod Menu BG Color", "#181b22", "Custom hex color for mod menu background.");


            var configuredTheme = configTheme.Value;

            if(configuredTheme == "custom")
            {
                LoadCustomColorTheme();
            }
            else
            {
                if(themes.ContainsKey(configuredTheme))
                {
                    LoadColorTheme(themes[configuredTheme]);
                }
                else
                {
                    LoadColorTheme(themes["original"]);
                }
            }
        }

        public static void LoadCustomColorTheme()
        {
            ButtonColor = HexToColor(configButtonColor.Value, Color.white).Value;
            ButtonColor2 = HexToColor(configButtonColor2.Value, Color.white).Value;
            ButtonColor4 = HexToColor(configButtonColor4.Value, Color.white).Value;
            BGColor1 = HexToColor(configBGColor1.Value, Color.white).Value;
            BGColor2 = HexToColor(configBGColor2.Value, Color.white).Value;
            TabMenuBG = HexToColor(configTabMenuBG.Value, Color.white).Value;
            HacksMenuBG = HexToColor(configHacksMenuBG.Value, Color.white).Value;
        }

        public static void LoadColorTheme(ColorTheme theme)
        {
            ButtonColor = theme.ButtonColor;
            ButtonColor2 = theme.ButtonColor2;
            ButtonColor4 = theme.ButtonColor4;
            BGColor1 = theme.BGColor1;
            BGColor2 = theme.BGColor2;
            TabMenuBG = theme.TabMenuBG;
            HacksMenuBG = theme.HacksMenuBG;
        }

        private static Color? HexToColor(string hex, Color? defaultResult = null)
        {
            if (!hex.StartsWith("#"))
            {
                hex = $"#{hex}";
            }

            if (ColorUtility.TryParseHtmlString(hex, out Color color))
            {
                return color;
            }

            Debug.LogWarning($"Invalid hex color: {hex}");
            return defaultResult;
        }


        public HacksUIHelper(GameObject root)
        {
            this.root = root;
        }

        public InputFieldRef CreateInputField(string placeholder, string name = "")
        {
            var group = UIFactory.CreateHorizontalGroup(root, name, true, true, true, true);
            UIFactory.SetLayoutElement(group);

            var inputFieldRef = UIFactory.CreateInputField(group, name, placeholder);
            inputFieldRef.Component.image.sprite = RoundedRect;
            inputFieldRef.Component.textComponent.font = Font;
            inputFieldRef.PlaceholderText.font = Font;

            UIFactory.SetLayoutElement(inputFieldRef.GameObject, 256, 32, 0, 0);

            var keybinder = inputFieldRef.GameObject.AddComponent<InputFieldKeybinder>();
            keybinder.input = inputFieldRef;
            keybinder.keybinderID = $"{new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.DeclaringType?.FullName}::{name}";
            keybinder.LoadKeybinds();

            return inputFieldRef;
        }

        public InputFieldRef CreateInputField(string placeholder, string name = "", int inputWidth = 256, int height = 32)
        {
            var group = UIFactory.CreateHorizontalGroup(root, name, true, true, true, true);
            UIFactory.SetLayoutElement(group);

            var inputFieldRef = UIFactory.CreateInputField(group, name, placeholder);
            inputFieldRef.Component.image.sprite = RoundedRect;
            inputFieldRef.Component.textComponent.font = Font;
            inputFieldRef.PlaceholderText.font = Font;

            UIFactory.SetLayoutElement(inputFieldRef.GameObject, inputWidth, height, 0, 0);

            var keybinder = inputFieldRef.GameObject.AddComponent<InputFieldKeybinder>();
            keybinder.input = inputFieldRef;
            keybinder.keybinderID = $"{new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.DeclaringType?.FullName}::{name}";
            keybinder.LoadKeybinds();

            return inputFieldRef;
        }

        public GameObject AddSpacer(int height = 0, int width = 0)
        {
            GameObject gameObject = UIFactory.CreateUIObject("Spacer", root);
            UIFactory.SetLayoutElement(gameObject, width, height, 0, 0);
            return gameObject;
        }

        public Text CreateLabel(string text, string name = "", TextAnchor alignment = TextAnchor.MiddleLeft, Color? color = null, bool richTextSupport = true, int fontSize = 16)
        {
            if (name == "")
            {
                name = text;
            }

            if (!color.HasValue)
            {
                color = Color.white;
            }

            var group = UIFactory.CreateHorizontalGroup(root, name, true, true, true, true);
            UIFactory.SetLayoutElement(group, 256 * 3, 32, 9999);

            var spacer = UIFactory.CreateUIObject("spacer", group);
            UIFactory.SetLayoutElement(spacer, 4);

            var text2 = UIFactory.CreateLabel(group, name, text, alignment, color.Value, richTextSupport, fontSize);
            text2.font = Font;
            UIFactory.SetLayoutElement(text2.gameObject, 256 * 3, 32, 9999);
            return text2;
        }

        /// <summary>
        /// Don't
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Slider CreateSlider(string name)
        {
            var group = UIFactory.CreateHorizontalGroup(root, name, true, true, true, true);
            UIFactory.SetLayoutElement(group);

            var gameObject = UIFactory.CreateSlider(group, name, out var slider);

            foreach(var image in slider.GetComponentsInChildren<Image>())
            {
                image.sprite = RoundedRect;
                image.type = Image.Type.Sliced;
            }

            UIFactory.SetLayoutElement(gameObject, 256 * 3, 32, 0, 0);

            var keybinder = gameObject.AddComponent<SliderKeybinder>();
            keybinder.slider = gameObject.GetComponent<Slider>();
            keybinder.keybinderID = $"{new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.DeclaringType?.FullName}::{name}";
            keybinder.LoadKeybinds();

            return slider;
        }

        public Toggle CreateToggle(string name = "", string label = "", UnityAction<bool> onValueChanged = null, bool defaultState = false, Color bgColor = default, int checkWidth = 20, int checkHeight = 20)
        {
            var group = UIFactory.CreateHorizontalGroup(root, name, true, true, true, true);
            UIFactory.SetLayoutElement(group);

            var spacer = UIFactory.CreateUIObject("spacer", group);
            UIFactory.SetLayoutElement(spacer, 4);

            var gameObject = UIFactory.CreateToggle(group, name, out var toggle, out var text, bgColor, checkWidth, checkHeight);
            text.text = label;
            text.font = Font;
            toggle.isOn = defaultState;
            toggle.onValueChanged.AddListener(onValueChanged);

            ((Image)toggle.targetGraphic).sprite = RoundedRect;
            ((Image)toggle.graphic).sprite = RoundedRect;

            UIFactory.SetLayoutElement(gameObject, 768, 32, 0, 0);
            gameObject.transform.Find("Background").Find("Checkmark").GetComponent<Image>().color = new Color(.565f, .792f, .976f);

            var keybinder = gameObject.AddComponent<ToggleKeybinder>();
            keybinder.toggle = toggle;
            keybinder.keybinderID = $"{new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.DeclaringType?.FullName}::{name}";
            keybinder.LoadKeybinds();

            return toggle;
        }

        public ButtonRef CreateButton(string text, Action onClick, string name = "", Color? color = null, int buttonWidth = 256, int height = 32)
        {
            if(color == null)
            {
                color = ButtonColor;
            }

            var group = UIFactory.CreateHorizontalGroup(root, name, true, true, true, true);
            UnityEngine.Object.Destroy(group.GetComponent<Image>());
            UIFactory.SetLayoutElement(group);

            var button = UIFactory.CreateButton(group, text + " Button", text, color);
            button.OnClick = onClick;
            button.Component.image = button.Transform.GetComponent<Image>();
            button.Component.image.sprite = RoundedRect;
            button.ButtonText.font = Font;

            var buttonKeybinder = button.GameObject.AddComponent<ButtonKeybinder>();
            buttonKeybinder.button = button;
            buttonKeybinder.keybinderID = $"{new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.DeclaringType?.FullName}::{name}";
            buttonKeybinder.LoadKeybinds();

            UIFactory.SetLayoutElement(button.GameObject, buttonWidth, height, 0, 0);

            return button;
        }

        /// <summary>
        /// Creates a label, input and button next to each other horizontally
        /// </summary>
        /// <returns>
        /// A reference to the three components
        /// </returns>
        public LIBTrio CreateLIBTrio(string label, string name = null, string inputPlaceHolder = "", Action onClick = null, string buttonText = "Apply", int labelWidth = 256, int inputWidth = 256, 
            int spacerWidth = 32, int buttonWidth = 256, int height = 32)
        {
            if (name == null) name = label;

            var group = UIFactory.CreateHorizontalGroup(root, name, true, true, true, true);
            UIFactory.SetLayoutElement(group);

            var _label = UIFactory.CreateLabel(group, label + " Label", " " + label);

            UIFactory.SetLayoutElement(_label.gameObject, labelWidth, height);

            var spacer1 = UIFactory.CreateUIObject("spacer1", group);
            UIFactory.SetLayoutElement(spacer1, spacerWidth);

            var input = UIFactory.CreateInputField(group, inputPlaceHolder + " Input", " " + inputPlaceHolder);
            input.Component.image.sprite = RoundedRect;
            input.Component.textComponent.font = Font;
            input.PlaceholderText.font = Font;

            UIFactory.SetLayoutElement(input.GameObject, inputWidth, height, 0, 0);

            var spacer2 = UIFactory.CreateUIObject("spacer2", group);
            UIFactory.SetLayoutElement(spacer2, spacerWidth);

            var button = UIFactory.CreateButton(group, buttonText + " Button", buttonText, ButtonColor);
            button.OnClick = onClick;

            button.Component.image = button.Transform.GetComponent<Image>();
            button.Component.image.sprite = RoundedRect;

            UIFactory.SetLayoutElement(button.GameObject, buttonWidth, height, 0, 0);

            var keybinder = _label.gameObject.AddComponent<LIBKeybinder>();
            keybinder.input = input;
            keybinder.button = button;
            keybinder.keybinderID = $"{new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.DeclaringType?.FullName}::{name}";
            keybinder.LoadKeybinds();

            return new(_label, input, button);
        }

        public class LIBTrio
        {
            public Text Label { get; private set; }
            public InputFieldRef Input { get; private set; }
            public ButtonRef Button { get; private set; }

            public LIBTrio(Text label, InputFieldRef input, ButtonRef button)
            {
                Label = label;
                Input = input;
                Button = button;
            }
        }

        /// <summary>
        /// Creates a label, dropdown and button next to each other horizontally
        /// </summary>
        /// <returns>
        /// A reference to the three components
        /// </returns>
        public LDBTrio CreateLDBTrio(string label, string name = null, string defaultItemText = "", int itemFontSize = 16, Action<int> onValueChanged = null, Action onClick = null, 
            string buttonText = "Apply", int labelWidth = 256, int dropdownWidth = 256, int spacerWidth = 32, int buttonWidth = 256, int height = 32)
        {
            if (name == null) name = label;

            var group = UIFactory.CreateHorizontalGroup(root, name, true, true, true, true);
            UIFactory.SetLayoutElement(group);

            var _label = UIFactory.CreateLabel(group, label + " Label", " " + label);
            UIFactory.SetLayoutElement(_label.gameObject, labelWidth, height);

            var spacer1 = UIFactory.CreateUIObject("spacer1", group);
            UIFactory.SetLayoutElement(spacer1, spacerWidth);

            var dropdownObj = UIFactory.CreateDropdown(group, "Dropdown", out var dropdown, defaultItemText, itemFontSize, onValueChanged);
            dropdown.image.sprite = RoundedRect;
            dropdown.captionText.font = Font;
            dropdown.itemText.font = Font;
            UIFactory.SetLayoutElement(dropdownObj, dropdownWidth, height, 0, 0);

            var spacer2 = UIFactory.CreateUIObject("spacer2", group);
            UIFactory.SetLayoutElement(spacer2, spacerWidth);

            var button = UIFactory.CreateButton(group, buttonText + " Button", buttonText, ButtonColor);
            button.OnClick = onClick;
            button.Component.image = button.Transform.GetComponent<UnityEngine.UI.Image>();
            button.Component.image.sprite = RoundedRect;
            button.ButtonText.font = Font;

            UIFactory.SetLayoutElement(button.GameObject, buttonWidth, height, 0, 0);

            var ldb = new LDBTrio(_label, dropdown, button);

            var keybinder = _label.gameObject.AddComponent<LDBKeybinder>();
            keybinder.LDB = ldb;
            keybinder.keybinderID = $"{new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.DeclaringType?.FullName}::{name}";
            keybinder.LoadKeybinds();

            return ldb;
        }

        public class LDBTrio
        {
            public Text Label { get; private set; }
            public Dropdown Dropdown { get; private set; }
            public ButtonRef Button { get; private set; }

            public LDBTrio(Text label, Dropdown dropdown, ButtonRef button)
            {
                Label = label;
                Dropdown = dropdown;
                Button = button;
            }
        }

        /// <summary>
        /// Creates a label and two buttons next to each other horizontally
        /// </summary>
        /// <returns>
        /// A reference to the three components
        /// </returns>
        public LBBTrio CreateLBBTrio(string label, string name = null, Action onClick1 = null, string buttonText1 = "Apply", string buttonName1 = null, Action onClick2 = null, 
            string buttonText2 = "Apply", string buttonName2 = null, int labelWidth = 256, int spacerWidth = 32, int button1Width = 256, int button2Width = 256, int height = 32)
        {
            name ??= label;
            buttonName1 ??= buttonText1;
            buttonName2 ??= buttonText2;

            var group = UIFactory.CreateHorizontalGroup(root, name, true, true, true, true);
            UIFactory.SetLayoutElement(group);

            var _label = UIFactory.CreateLabel(group, label + " Label", " " + label);
            UIFactory.SetLayoutElement(_label.gameObject, labelWidth, height);

            var spacer1 = UIFactory.CreateUIObject("spacer", group);
            UIFactory.SetLayoutElement(spacer1, spacerWidth);

            var button1 = UIFactory.CreateButton(group, buttonName1, buttonText1, ButtonColor);
            button1.OnClick = onClick1;
            button1.Component.image = button1.Transform.GetComponent<Image>();
            button1.Component.image.sprite = RoundedRect;
            button1.ButtonText.font = Font;

            var buttonKeybinder = button1.GameObject.AddComponent<ButtonKeybinder>();
            buttonKeybinder.button = button1;
            buttonKeybinder.keybinderID = $"{new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.DeclaringType?.FullName}::{name}_{buttonName1}";
            buttonKeybinder.LoadKeybinds();

            UIFactory.SetLayoutElement(button1.GameObject, button1Width, height, 0, 0);

            var spacer2 = UIFactory.CreateUIObject("spacer", group);
            UIFactory.SetLayoutElement(spacer2, spacerWidth);

            var button2 = UIFactory.CreateButton(group, buttonName2, buttonText2, ButtonColor);
            button2.OnClick = onClick2;
            button2.Component.image = button2.Transform.GetComponent<Image>();
            button2.Component.image.sprite = RoundedRect;
            button2.ButtonText.font = Font;

            var buttonKeybinder2 = button2.GameObject.AddComponent<ButtonKeybinder>();
            buttonKeybinder2.button = button2;
            buttonKeybinder2.keybinderID = $"{new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.DeclaringType?.FullName}::{name}2_{buttonName2}";
            buttonKeybinder2.LoadKeybinds();

            UIFactory.SetLayoutElement(button2.GameObject, button2Width, height, 0, 0);

            return new(_label, button1, button2);
        }

        public class LBBTrio
        {
            public Text Label { get; private set; }
            public ButtonRef Button { get; private set; }
            public ButtonRef Button2 { get; private set; }

            public LBBTrio(Text label, ButtonRef button, ButtonRef button2)
            {
                Label = label;
                Button = button;
                Button2 = button2;
            }
        }

        /// <summary>
        /// Creates a label and a button next to eachother horizontally
        /// </summary>
        /// <returns>
        /// A reference to the two components
        /// </returns>
        public LBDuo CreateLBDuo(string label, string name = null, Action onClick = null, string buttonText = "Apply", string buttonName = null, int labelWidth = 256, int spacerWidth = 32, int buttonWidth = 256, int height = 32)
        {
            if (name == null) name = label;
            if (buttonName == null) buttonName = buttonText + " Button";

            var group = UIFactory.CreateHorizontalGroup(root, name, true, true, true, true);
            UIFactory.SetLayoutElement(group);

            var _label = UIFactory.CreateLabel(group, label + " Label", " " + label);
            UIFactory.SetLayoutElement(_label.gameObject, labelWidth, height);

            var spacer = UIFactory.CreateUIObject("spacer", group);
            UIFactory.SetLayoutElement(spacer, spacerWidth);

            var button = UIFactory.CreateButton(group, buttonName, buttonText, ButtonColor);
            button.OnClick = onClick;
            button.Component.image = button.Transform.GetComponent<Image>();
            button.Component.image.sprite = RoundedRect;
            button.ButtonText.font = Font;

            var buttonKeybinder = button.GameObject.AddComponent<ButtonKeybinder>();
            buttonKeybinder.button = button;
            buttonKeybinder.keybinderID = $"{new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.DeclaringType?.FullName}::{name}";
            buttonKeybinder.LoadKeybinds();

            UIFactory.SetLayoutElement(button.GameObject, buttonWidth, height, 0, 0);

            return new(_label, button);
        }

        public class LBDuo
        {
            public Text Label { get; private set; }
            public ButtonRef Button { get; private set; }

            public LBDuo(Text label, ButtonRef button)
            {
                Label = label;
                Button = button;
            }
        }

        /// <summary>
        /// Creates a label and input next to each other horizontally
        /// </summary>
        /// <returns>
        /// A reference to the two components
        /// </returns>
        public LIDuo CreateLIDuo(string label, string name = null, string inputName = "Input", string inputPlaceholder = "", InputField.CharacterValidation characterValidation = InputField.CharacterValidation.None, 
            int labelWidth = 256, int spacerWidth = 32, int inputWidth = 256, int height = 32)
        {
            name ??= label;

            var group = UIFactory.CreateHorizontalGroup(root, name, true, true, true, true);
            UIFactory.SetLayoutElement(group);

            var _label = UIFactory.CreateLabel(group, label + " Label", " " + label);
            UIFactory.SetLayoutElement(_label.gameObject, labelWidth, height);

            var spacer = UIFactory.CreateUIObject("spacer", group);
            UIFactory.SetLayoutElement(spacer, spacerWidth);

            var input = UIFactory.CreateInputField(group, inputName, " " + inputPlaceholder);
            input.Component.image.sprite = RoundedRect;
            input.Component.textComponent.font = Font;

            UIFactory.SetLayoutElement(input.GameObject, inputWidth, height, 0, 0);

            var keybinder = input.GameObject.AddComponent<InputFieldKeybinder>();
            keybinder.input = input;
            keybinder.keybinderID = $"{new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.DeclaringType?.FullName}::{name}";
            keybinder.LoadKeybinds();

            return new(_label, input);
        }

        public class LIDuo
        {
            public Text Label { get; private set; }
            public InputFieldRef Input { get; private set; }

            public LIDuo(Text label, InputFieldRef input)
            {
                Label = label;
                Input = input;
            }
        }

        public GameObject CreateGridGroup(string name, Vector2 cellSize, Vector2 spacing, Color bgColor = default)
        {
            var obj = UIFactory.CreateGridGroup(root, name, cellSize, spacing, bgColor);
            UIFactory.SetLayoutElement(obj, 25, 25, 999);
            return obj;
        }

        public GameObject CreateHorizontalGroup(string name, bool forceExpandWidth, bool forceExpandHeight, bool childControlWidth, bool childControlHeight, int spacing = 0, Vector4 padding = default,
            Color bgColor = default, TextAnchor? childAlignment = null)
        {
            var obj = UIFactory.CreateHorizontalGroup(root, name, forceExpandWidth, forceExpandHeight, childControlWidth, childControlHeight, spacing, padding, bgColor, childAlignment);
            UIFactory.SetLayoutElement(obj, 25, 25, 9999);
            return obj;
        }

        public GameObject CreateVerticalGroup(string name, bool forceExpandWidth, bool forceExpandHeight, bool childControlWidth, bool childControlHeight, int spacing = 0, Vector4 padding = default,
            Color bgColor = default, TextAnchor? childAlignment = null)
        {
            var obj = UIFactory.CreateVerticalGroup(root, name, forceExpandWidth, forceExpandHeight, childControlWidth, childControlHeight, spacing, padding, bgColor, childAlignment);
            UIFactory.SetLayoutElement(obj, 25, 25, 9999);
            return obj;
        }

        public Dropdown CreateDropdown(string name, Action<int> onValueChanged, string defaultItemText = "", int itemFontSize = 16, string[] defaultOptions = null)
        {
            var obj = UIFactory.CreateDropdown(root, name, out var dropdown, defaultItemText, itemFontSize, onValueChanged, defaultOptions);
            dropdown.image.sprite = RoundedRect;
            dropdown.captionText.font = Font;
            dropdown.itemText.font = Font;
            UIFactory.SetLayoutElement(obj, 25, 25, 9999);

            var keybinder = dropdown.gameObject.AddComponent<DropdownKeybinder>();
            keybinder.dropdown = dropdown;
            keybinder.keybinderID = $"{new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.DeclaringType?.FullName}::{name}";
            keybinder.LoadKeybinds();

            return dropdown;
        }

        /// <summary>
        /// Color for regular buttons
        /// </summary>
        public static Color ButtonColor { get; private set; } = new Color(0.22f, 0.25f, 0.31f);

        /// <summary>
        /// Color for tab buttons
        /// </summary>
        public static Color ButtonColor2 { get; private set; } = new Color(.063f, .094f, .129f);

        /// <summary>
        /// Color for special buttons
        /// </summary>
        public static Color ButtonColor4 { get; private set; } = new Color(0.15f, 0.38f, 0.55f);

        public static Color BGColor1 { get; private set; } = new Color(.129f, .145f, .176f);
        public static Color BGColor2 { get; private set; } = new Color(.114f, .129f, .161f);

        public static Color TabMenuBG { get; private set; } = new Color(.102f, .157f, .216f);
        public static Color HacksMenuBG { get; private set; } = new Color(.095f, .108f, .133f);

        public static readonly ColorTheme originalTheme = new()
        {
            ButtonColor = new Color(0.22f, 0.25f, 0.31f),
            ButtonColor2 = new Color(.063f, .094f, .129f),
            ButtonColor4 = new Color(0.15f, 0.38f, 0.55f),
            BGColor1 = new Color(.129f, .145f, .176f),
            BGColor2 = new Color(.114f, .129f, .161f),
            TabMenuBG = new Color(.102f, .157f, .216f),
            HacksMenuBG = new Color(.095f, .108f, .133f)
        };

        public struct ColorTheme
        {
            /// <summary>
            /// Color for regular buttons
            /// </summary>
            public Color ButtonColor;

            /// <summary>
            /// Color for tab buttons
            /// </summary>
            public Color ButtonColor2;

            /// <summary>
            /// Color for special buttons
            /// </summary>
            public Color ButtonColor4;

            public Color BGColor1;
            public Color BGColor2;

            public Color TabMenuBG;
            public Color HacksMenuBG;
        }
    }
}
