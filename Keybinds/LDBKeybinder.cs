using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;
using UniverseLib.UI.Models;

namespace lstwoMODS_Core.Keybinds
{
    public class LDBKeybinder : Keybinder
    {
        public HacksUIHelper.LDBTrio LDB;

        public override void OnRightClicked()
        {
            Plugin.KeybindPanel._SetActive(true);
            Plugin.KeybindPanel.Keybinder = this;
        }

        public override Keybind CreateKeybind()
        {
            var keybind = new LDBKeybind(this);
            keybinds.Add(keybind);
            return keybind;
        }

        public class LDBKeybind : Keybind
        {
            public LDBKeybind(Keybinder keybinder) : base(keybinder)
            {

            }

            public override void OnPressed()
            {
                var dropdownKeybinder = keybinder as LDBKeybinder;

                if (dropdownKeybinder != null)
                {
                    dropdownKeybinder.LDB.Dropdown.value = keybindValue;
                    dropdownKeybinder.LDB.Button.OnClick.Invoke();
                }
            }

            private Text text;
            private int keybindValue = 0;
            private Dropdown dropdown;

            public override void CreateScrollItem(GameObject root)
            {
                UIFactory.SetLayoutElement(UIFactory.CreateUIObject("spacer", root), 0, 6, 9999, 0);

                var dropdownGroup = UIFactory.CreateUIObject("dropdownGroup", root);
                UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(dropdownGroup, false, false, true, true);

                UIFactory.SetLayoutElement(UIFactory.CreateUIObject("spacer", dropdownGroup), 6, 0, 0, 9999);

                var button = UIFactory.CreateButton(dropdownGroup, "button", "Set Keybind", HacksUIHelper.ButtonColor);
                button.OnClick = StartDetectKeybind;
                button.GameObject.GetComponent<Image>().sprite = HacksUIHelper.RoundedRect;
                UIFactory.SetLayoutElement(button.GameObject, 128, 48, 0, 0);

                UIFactory.SetLayoutElement(UIFactory.CreateUIObject("spacer", dropdownGroup), 6, 0, 0, 9999);

                text = UIFactory.CreateLabel(dropdownGroup, "text", "No Keys Selected");
                UIFactory.SetLayoutElement(text.gameObject, 9999, 48, 9999, 0);

                UIFactory.SetLayoutElement(UIFactory.CreateUIObject("spacer", root), 0, 6, 9999, 0);

                var inputGroup = UIFactory.CreateHorizontalGroup(root, "inputGroup", false, false, true, true);
                UIFactory.SetLayoutElement(inputGroup, 0, 0, 9999, 9999);

                UIFactory.SetLayoutElement(UIFactory.CreateUIObject("spacer", inputGroup), 6, 0, 0, 9999);

                var dropdownObj = UIFactory.CreateDropdown(inputGroup, "input", out dropdown, "Input for keybind", 16, (index) => keybindValue = index);
                dropdown.image.sprite = HacksUIHelper.RoundedRect;
                UIFactory.SetLayoutElement(dropdownObj, 555, 32, 0, 0);

                UIFactory.SetLayoutElement(UIFactory.CreateUIObject("spacer", inputGroup), 6, 0, 0, 9999);

                UIFactory.SetLayoutElement(UIFactory.CreateUIObject("spacer", root), 0, 6, 9999, 0);
            }

            public override void RefreshScrollItem()
            {
                dropdown.options = (keybinder as DropdownKeybinder).dropdown.options;
                dropdown.value = keybindValue;

                if(!primaryKey.HasValue)
                {
                    return;
                }

                var _text = "";
                secondaryKeys.ForEach(key => _text += key.ToString() + " + ");
                _text += primaryKey.ToString();

                text.text = _text;
            }

            public override void StartDetectKeybind()
            {
                base.StartDetectKeybind();
                text.text = "Press Any Key...";
            }

            public override void StopDetectKeybind()
            {
                base.StopDetectKeybind();
                
                if(primaryKey == null)
                {
                    text.text = "No Keys Selected";
                    return;
                }

                var _text = "";
                secondaryKeys.ForEach(key => _text += key.ToString() + " + ");
                _text += primaryKey.ToString();

                text.text = _text;
            }

            public override string[] SerializeData()
            {
                return new string[] { keybindValue.ToString() };
            }

            public override void DeserializeData(string[] data)
            {
                keybindValue = int.Parse(data[0]);
            }
        }
    }
}
