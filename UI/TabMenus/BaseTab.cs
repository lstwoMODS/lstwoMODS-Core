using System;
using ImGuiNET;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;

namespace lstwoMODS_Core.UI.TabMenus
{
    public abstract class BaseTab
    {
        public BaseTab()
        {
            Plugin.TabMenus.Add(this);
        }

        /// <summary>
        /// The name showed on the tab.
        /// </summary>
        public string Name;

        /// <summary>
        /// Called every frame to render the UI.
        /// </summary>
        /// <param name="root"></param>
        public abstract void RenderUI();

        /// <summary>
        /// Called when the tab gets opened.
        /// </summary>
        public abstract void RefreshUI();
    }
}
