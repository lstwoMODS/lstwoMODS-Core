using lstwoMODS_Core.UI.TabMenus;
using UnityEngine;

namespace lstwoMODS_Core.Hacks
{
    public abstract class BaseMod
    {
        /// <summary>
        /// The name shown on the Hack button.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Text shown as tooltip when hovering
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// The tab your hack should get added to.
        /// </summary>
        public abstract ModsTab ModsTab { get; }

        public BaseMod()
        {
            Plugin.Mods.Add(this);

            if(ModsTab != null)
            {
                ModsTab.Mods.Add(this);
            }
        }

        /// <summary>
        /// Called when UI gets constructed. Use this to create your UI.
        /// </summary>
        /// <param name="root">The root layout group. Place your objects as children of this object.</param>
        public abstract void RenderUI();

        /// <summary>
        /// Called every frame. Use for special mods that need this.
        /// </summary>
        public abstract void Update();

        /// <summary>
        /// Called when the UI, Tab or Mod is opened / closed. Use this to refresh your UI values.
        /// </summary>
        public abstract void RefreshUI();
    }
}
