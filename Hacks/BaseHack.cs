using lstwoMODS_Core.UI.TabMenus;
using UnityEngine;

namespace lstwoMODS_Core.Hacks
{
    public abstract class BaseHack
    {
        /// <summary>
        /// The name showed on the Hack button.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Unused for now.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// The tab your hack should get added to.
        /// </summary>
        public abstract HacksTab HacksTab { get; }

        public BaseHack()
        {
            Plugin.Hacks.Add(this);

            if(HacksTab != null)
            {
                HacksTab.Hacks.Add(this);
            }
        }

        /// <summary>
        /// Called when UI gets constructed. Use this to create your UI.
        /// </summary>
        /// <param name="root">The root layout group. Place your objects as children of this object.</param>
        public abstract void ConstructUI(GameObject root);

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
