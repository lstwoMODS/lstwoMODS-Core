using System;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;

namespace lstwoMODS_Core.UI.TabMenus;

public class ProfilesTab : TabWithIcon
{
    private GameObject gridRoot;
    private GridLayoutGroup gridView;

    public ProfilesTab(Sprite icon) : base(icon)
    {
        Name = "Profiles";
    }

    /*public override void ConstructUI(GameObject root)
    {
        base.ConstructUI(root);

        gridRoot = UIFactory.CreateGridGroup(root, "gridView", new(268, 436 - 128), new(6, 6), HacksUIHelper.HacksMenuBG);
        gridView = gridRoot.GetComponent<GridLayoutGroup>();
    }*/
}

[Serializable]
public class Profile
{
    
}