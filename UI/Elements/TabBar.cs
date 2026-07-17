using System;
using System.Collections.Generic;
using System.Linq;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class TabBar : BaseUIElement<TabBar>
{
    public List<BaseUIElement> Children;

    public TabBar(string name, params TabItem[] tabs) : base(name)
    {
        Children = new List<BaseUIElement>(tabs);
        Data = new TabBarData
        {
            Name     = name,
            Children = Children.Select(c => c.Data).ToList()
        };
    }

    public TabBar WithFlags(ImGuiTabBarFlags flags) { ((TabBarData)Data).Flags = flags; return this; }

    public override IEnumerable<BaseUIElement> GetChildren() => Children;
}

public class TabItem : BaseUIElement<TabItem>
{
    public List<BaseUIElement> Children;
    public Action<bool>? OnClose; // fires when tab is closed via X button

    public TabItem(string name, string label, params BaseUIElement[] children) : base(name)
    {
        Children = new List<BaseUIElement>(children);
        Data = new TabItemData
        {
            Name     = name,
            Label    = label,
            Open     = true,
            Children = Children.Select(c => c.Data).ToList()
        };
    }

    /// <summary>Show an X close button on the tab. Chainable.</summary>
    public TabItem WithClose(Action<bool> onClose = null, bool mainThread = true)
    {
        ((TabItemData)Data).ShowClose = true;
        OnClose = onClose;
        RunCallbacksOnMainThread = mainThread;
        return this;
    }

    public TabItem WithFlags(ImGuiTabItemFlags flags) { ((TabItemData)Data).Flags = flags; return this; }

    public override IEnumerable<BaseUIElement> GetChildren() => Children;

    public override void ApplyReceivedData(BaseUIElementData data)
    {
        var wasOpen = ((TabItemData)Data).Open;
        base.ApplyReceivedData(data);
        var nowOpen = ((TabItemData)Data).Open;
        if (wasOpen && !nowOpen)
            InvokeCallback(() => OnClose?.Invoke(false));
    }
}
