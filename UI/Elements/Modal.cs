using System;
using System.Collections.Generic;
using System.Linq;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;
public class Modal : BaseUIElement<Modal>
{
    public List<BaseUIElement> Children;
    public Action OnClosed;
    public bool IsOpen => ((ModalData)Data).IsOpen;

    public Modal(string name, string title, params BaseUIElement[] children) : base(name)
    {
        Children = new List<BaseUIElement>(children);
        Data = new ModalData { Name = name, Label = title, Children = Children.Select(c => c.Data).ToList() };
    }

    public void Open()  { ((ModalData)Data).IsOpen = true;  MarkChanged(); }
    public void Close() { ((ModalData)Data).IsOpen = false; MarkChanged(); }

    /// <summary>Change the modal's title (also its ImGui popup id). Set before <see cref="Open"/>;
    /// retitling an already-open modal resets its window position/size.</summary>
    public void SetTitle(string title) { ((ModalData)Data).Label = title; MarkChanged(); }

    public Modal WithFlags(ImGuiWindowFlags flags)    { ((ModalData)Data).Flags = flags; return this; }

    /// <summary>Initial window size (applied FirstUseEver  the modal stays resizable and a
    /// user resize sticks for the session). Required when content stretches to fill the modal,
    /// e.g. a fill-height <see cref="ChildWindow"/>; auto-fit can't measure stretchy children.
    /// Chainable.</summary>
    public Modal WithSize(float width, float height)  { var d = (ModalData)Data; d.SizeX = width; d.SizeY = height; return this; }
    public Modal WithNoClose()                         { ((ModalData)Data).HasClose = false; return this; }
    public Modal OnClose(Action cb)                    { OnClosed = cb; return this; }

    public override IEnumerable<BaseUIElement> GetChildren() => Children;

    public override void ApplyReceivedData(BaseUIElementData data)
    {
        var wasOpen = IsOpen;
        base.ApplyReceivedData(data);
        if (wasOpen && !IsOpen) InvokeCallback(() => OnClosed?.Invoke());
    }
}
