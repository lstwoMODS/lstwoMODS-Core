using System;
using System.Collections.Generic;
using System.Linq;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;
public class Popup : BaseUIElement<Popup>
{
    public List<BaseUIElement> Children;
    public Action OnClosed;
    public bool IsOpen => ((PopupData)Data).IsOpen;

    public Popup(string name, params BaseUIElement[] children) : base(name)
    {
        Children = new List<BaseUIElement>(children);
        Data = new PopupData { Name = name, Children = Children.Select(c => c.Data).ToList() };
    }

    /// <summary>Open the popup (takes effect next render frame).</summary>
    public void Open()  { ((PopupData)Data).IsOpen = true;  MarkChanged(); }
    /// <summary>Close the popup via a pending CloseCurrentPopup call next frame.</summary>
    public void Close() { ((PopupData)Data).IsOpen = false; MarkChanged(); }

    public Popup WithFlags(ImGuiWindowFlags flags) { ((PopupData)Data).Flags = flags; return this; }
    public Popup OnClose(Action cb) { OnClosed = cb; return this; }

    public override IEnumerable<BaseUIElement> GetChildren() => Children;

    public override void ApplyReceivedData(BaseUIElementData data)
    {
        var wasOpen = IsOpen;
        base.ApplyReceivedData(data);
        if (wasOpen && !IsOpen) InvokeCallback(() => OnClosed?.Invoke());
    }
}
