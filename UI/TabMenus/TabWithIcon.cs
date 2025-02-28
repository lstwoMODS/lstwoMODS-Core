using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;

namespace lstwoMODS_Core.UI.TabMenus;

public class TabWithIcon : BaseTab
{
    private Sprite icon;
    
    public TabWithIcon(Sprite icon) : base()
    {
        this.icon = icon;
    }
    
    public override void UpdateUI() { }

    public override void RefreshUI() { }

    public override GameObject ConstructTabButton(GameObject root)
    {
        var btn = UIFactory.CreateButton(root, Name, "<b>" + Name + "</b>", HacksUIHelper.ButtonColor2);
        btn.OnClick = () => SetTabActive(true);
        //btn.ButtonText.font = HacksUIHelper.Font;
        btn.GameObject.GetComponent<Image>().sprite = HacksUIHelper.RoundedRect;
        UIFactory.SetLayoutElement(btn.GameObject, 224, 36, 0, 0);

        if (icon == null)
        {
            return btn.GameObject;
        }
        
        
        var imageObj = new GameObject("icon", typeof(RectTransform));
        imageObj.transform.SetParent(btn.GameObject.transform);
        imageObj.transform.localPosition = Vector3.zero;
        
        var imageRect = imageObj.GetComponent<RectTransform>();
        imageRect.anchorMin = new(0, 0.5f);
        imageRect.anchorMax = new(0, 0.5f);
        imageRect.pivot = new(0.5f, 0.5f);
        imageRect.anchoredPosition = new(18, 0);
        imageRect.sizeDelta = new(24, 24);
        
        var image = imageObj.AddComponent<Image>();
        image.sprite = icon;

        return btn.GameObject;
    }
}