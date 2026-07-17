using lstwoMODS_Core.UI.Elements;

namespace lstwoMODS_Core.UI.TabMenus
{
    public abstract class BaseWindow
    {
        public BaseWindow()
        {
            LstwoModsPanels.Windows.Add(this);
        }

        /// <summary>
        /// The name showed on the tab.
        /// </summary>
        public string Name;

        /// <summary>
        /// Optional icon glyph (e.g. a <see cref="Lucide"/> constant) shown before
        /// <see cref="Name"/> in the window title / dock tab.
        /// </summary>
        public string TitleIcon;

        /// <summary>
        /// ImGui window title: "{icon} {Name}###{Name}". The "###{Name}" suffix keys the
        /// window's ID and saved layout, so changing the icon never resets the layout.
        /// </summary>
        public string WindowTitle => string.IsNullOrEmpty(TitleIcon)
            ? $"{Name}###{Name}"
            : $"{TitleIcon} {Name}###{Name}";

        /// <summary>
        /// Called on UI construction
        /// </summary>
        /// <param name="root"></param>
        public abstract Group ConstructUI();

        /// <summary>
        /// Called when the tab gets opened.
        /// </summary>
        public abstract void RefreshUI();
    }
}
