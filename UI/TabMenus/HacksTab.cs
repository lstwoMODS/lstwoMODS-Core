using lstwoMODS_Core.Hacks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UniverseLib.UI;

namespace lstwoMODS_Core.UI.TabMenus
{
    public class HacksTab : TabWithIcon
    {
        public List<BaseHack> Hacks = new();

        public HacksTab(Sprite icon, string name = "Mods") : base(icon)
        {
            Name = name;
        }

        public override void ConstructUI(GameObject root)
        {
            base.ConstructUI(root);

            var b = true;

            foreach (var hack in Hacks)
            {
                try
                {
                    GameObject newRoot = null;

                    new HacksUIHelper(root).AddSpacer(6);

                    var bgColor = b ? HacksUIHelper.BGColor1 : HacksUIHelper.BGColor2;

                    var fullHackRoot = UIFactory.CreateVerticalGroup(root, hack.Name, false, false, true, true, bgColor: bgColor);
                    UIFactory.SetLayoutElement(fullHackRoot);

                    var hackBtn = UIFactory.CreateButton(fullHackRoot, hack.Name + " Button", hack.Name, bgColor);
                    //hackBtn.ButtonText.font = HacksUIHelper.Font;
                    hackBtn.OnClick = () =>
                    {
                        newRoot.SetActive(!newRoot.activeSelf);

                        if (newRoot.activeSelf)
                        {
                            hack.RefreshUI();
                        }
                    };
                    UIFactory.SetLayoutElement(hackBtn.GameObject, 0, 28, 9999, 0);

                    newRoot = UIFactory.CreateVerticalGroup(fullHackRoot, hack.Name, true, true, false, true, bgColor: bgColor);
                    UIFactory.SetLayoutElement(newRoot);

                    b = !b;

                    new HacksUIHelper(newRoot).AddSpacer(6);

                    hack.ConstructUI(newRoot);

                    new HacksUIHelper(newRoot).AddSpacer(6);

                    newRoot.SetActive(false);
                } 
                catch (Exception e)
                {
                    Plugin.LogSource.LogError(e);
                }
            }
        }

        public override void RefreshUI()
        {
            foreach (var hack in Hacks)
            {
                try
                {
                    hack.RefreshUI();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        public override void UpdateUI()
        {

        }
    }
}
