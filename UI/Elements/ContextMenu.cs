using System;
using System.Collections.Generic;
using System.Linq;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

/// <summary>
/// Renders trigger content, then opens a context menu on right-click.
/// Use OnItem=true (default) to open on right-click of the previous rendered item,
/// or OnItem=false to open on right-click anywhere in the current window.
/// </summary>
public class ContextMenu : BaseUIElement<ContextMenu>
{
    public List<BaseUIElement> Trigger;
    public List<BaseUIElement> Items;

    public ContextMenu(string name, BaseUIElement[] trigger, params BaseUIElement[] items) : base(name)
    {
        Trigger = new List<BaseUIElement>(trigger);
        Items   = new List<BaseUIElement>(items);
        Data = new ContextMenuData
        {
            Name    = name,
            Trigger = Trigger.Select(c => c.Data).ToList(),
            Items   = Items.Select(c => c.Data).ToList()
        };
    }

    /// <summary>Shortcut: single trigger element.</summary>
    public ContextMenu(string name, BaseUIElement trigger, params BaseUIElement[] items)
        : this(name, new[] { trigger }, items) { }

    public ContextMenu WithFlags(ImGuiPopupFlags flags) { ((ContextMenuData)Data).PopupFlags = flags; return this; }
    public ContextMenu OnWindow() { ((ContextMenuData)Data).OnItem = false; return this; }

    public override IEnumerable<BaseUIElement> GetChildren()
        => System.Linq.Enumerable.Concat(Trigger, Items);
}

/// <summary>ImGui.MenuItem  a clickable item inside a popup, context menu, or menu bar menu.</summary>
public class MenuItem : BaseUIElement<MenuItem>
{
    public Action OnClicked;
    public bool   Selected { get => ((MenuItemData)Data).Selected; set { var d = (MenuItemData)Data; d.Selected = value; d.Checkable = true; MarkChanged(); } }

    public MenuItem(string name, Action onClicked = null, bool mainThread = true) : base(name)
    {
        Data = new MenuItemData { Name = name };
        OnClicked = onClicked;
        RunCallbacksOnMainThread = mainThread;
    }

    public MenuItem WithShortcut(string shortcut)      { ((MenuItemData)Data).Shortcut    = shortcut; return this; }
    public MenuItem WithSelected(bool selected)         { var d = (MenuItemData)Data; d.Selected = selected; d.Checkable = true; return this; }
    public MenuItem WithItemEnabled(bool enabled)       { ((MenuItemData)Data).ItemEnabled = enabled;  return this; }
    public MenuItem OnClick(Action cb, bool mainThread = true) { OnClicked = cb; RunCallbacksOnMainThread = mainThread; return this; }

    public override void ApplyReceivedData(BaseUIElementData data)
    {
        base.ApplyReceivedData(data);
        if (((MenuItemData)data).Clicked)
            InvokeCallback(() => OnClicked?.Invoke());
    }
}

/// <summary>ImGui.BeginMenu  a sub-menu that appears on hover inside a popup or menu bar.</summary>
public class Menu : BaseUIElement<Menu>
{
    public List<BaseUIElement> Children;

    public Menu(string name, string label, params BaseUIElement[] children) : base(name)
    {
        Children = new List<BaseUIElement>(children);
        Data = new MenuData { Name = name, Label = label, Children = Children.Select(c => c.Data).ToList() };
    }

    public Menu WithEnabled(bool enabled) { ((MenuData)Data).MenuEnabled = enabled; return this; }
    public override IEnumerable<BaseUIElement> GetChildren() => Children;
}

/// <summary>ImGui.BeginMenuBar  renders a menu bar at the top of the parent window. Requires ImGuiWindowFlags.MenuBar on the window.</summary>
public class MenuBar : BaseUIElement<MenuBar>
{
    public List<BaseUIElement> Children;

    public MenuBar(string name, params BaseUIElement[] children) : base(name)
    {
        Children = new List<BaseUIElement>(children);
        Data = new MenuBarData { Name = name, Children = Children.Select(c => c.Data).ToList() };
    }

    public override IEnumerable<BaseUIElement> GetChildren() => Children;
}

/// <summary>ImGui.BeginMainMenuBar  full-width menu bar fixed at the top of the display (not tied to any window).</summary>
public class MainMenuBar : BaseUIElement<MainMenuBar>
{
    public List<BaseUIElement> Children;

    public MainMenuBar(string name, params BaseUIElement[] children) : base(name)
    {
        Children = new List<BaseUIElement>(children);
        Data = new MainMenuBarData { Name = name, Children = Children.Select(c => c.Data).ToList() };
    }

    public override IEnumerable<BaseUIElement> GetChildren() => Children;
}

/// <summary>Calls ImGui.CloseCurrentPopup(). Place inside a Popup or Modal to close it on a button press or menu item click.</summary>
public class ClosePopup : BaseUIElement<ClosePopup>
{
    public ClosePopup(string name) : base(name)
    {
        Data = new ClosePopupData { Name = name };
    }
}
