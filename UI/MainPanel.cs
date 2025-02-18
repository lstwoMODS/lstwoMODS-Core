using lstwoMODS_Core.UI.TabMenus;
using UnityEngine;
using UniverseLib.UI;
using UniverseLib.UI.Panels;

namespace lstwoMODS_Core.UI
{
    public class MainPanel : PanelBase
    {
        public MainPanel(UIBase owner) : base(owner) { }

        public override string Name => "<b>lstwoMODS</b>";

        public override int MinWidth => 1280;
        public override int MinHeight => 720;

        public override Vector2 DefaultAnchorMin => default;
        public override Vector2 DefaultAnchorMax => default;

        public override Vector2 DefaultPosition => new(-MinWidth / 2, MinHeight / 2);

        public BaseTab CurrentTab { get; set; }
        public BaseTab oldTab = null;

        protected override void ConstructPanelContent()
        {
            var ui = new HacksUIHelper(ContentRoot);

            var horizontalGroup = ui.CreateHorizontalGroup("horizontalGroup", true, true, true, true);

            var fullTabMenu = UIFactory.CreateHorizontalGroup(horizontalGroup, "full tab menu", false, false, true, true, bgColor: HacksUIHelper.TabMenuBG);
            UIFactory.SetLayoutElement(fullTabMenu, 256, 1, 0, 9999);

            new HacksUIHelper(fullTabMenu).AddSpacer(0, 3);

            var tabMenu = UIFactory.CreateScrollView(fullTabMenu, "tabMenu", out var tabMenuRoot, out var tabMenuScrollbar, HacksUIHelper.TabMenuBG);
            UIFactory.SetLayoutElement(tabMenu, 253, 1, 0, 9999);

            var tabUi = new HacksUIHelper(tabMenuRoot);

            var hacksMenu = UIFactory.CreateScrollView(horizontalGroup, "hacksMenu", out var hacksMenuRoot, out var hacksMenuScrollbar, HacksUIHelper.HacksMenuBG);
            UIFactory.SetLayoutElement(hacksMenu, 512, 1, 9999, 9999);

            foreach (BaseTab tab in Plugin.TabMenus)
            {
                tabUi.AddSpacer(3);

                tab.ConstructTabButton(tabMenuRoot);

                var newRoot = UIFactory.CreateVerticalGroup(hacksMenuRoot, tab.Name, false, false, true, true, bgColor: HacksUIHelper.HacksMenuBG);

                tab.ConstructUI(newRoot);
            }
        }

        public void Refresh()
        {
            foreach(BaseTab tab in Plugin.TabMenus)
            {
                tab.RefreshUI();
            }
        }

        protected override void OnClosePanelClicked()
        {
            Plugin.UiBase.Enabled = false;
            Plugin.OnUIToggle(false);
        }
    }
}
