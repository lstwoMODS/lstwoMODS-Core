using System.Collections.Generic;
using System.Numerics;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class PopupRenderer : UIRenderer
{
    private bool _pendingOpen;
    private bool _pendingClose;
    private bool _wasOpen;
    private ImGuiWindowFlags _flags;
    private List<BaseUIElementData> _children;

    public PopupRenderer(BaseUIElementData data) : base(data)
    {
        var d = (PopupData)data;
        _flags    = (ImGuiWindowFlags)(int)d.Flags;
        _children = d.Children;
        if (d.IsOpen) _pendingOpen = true;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (PopupData)data;
        var wasOpen = _wasOpen;
        Data = d; Name = d.Name;
        _flags = (ImGuiWindowFlags)(int)d.Flags;
        if (d.Children?.Count > 0) _children = d.Children;
        // IsOpen going true → queue open; going false → queue close
        if (d.IsOpen && !wasOpen) _pendingOpen  = true;
        if (!d.IsOpen && wasOpen) _pendingClose = true;
    }

    // A closed popup draws nothing; its subtree must not count toward input capture.
    public override bool ParticipatesInInput => _wasOpen;

    public override void Render()
    {
        if (_pendingOpen)  { ImGui.OpenPopup(Name); _pendingOpen  = false; }

        if (!ImGui.BeginPopup(Name, _flags))
        {
            _wasOpen = false;
            return;
        }

        if (_pendingClose) { ImGui.CloseCurrentPopup(); _pendingClose = false; }
        _wasOpen = true;

        foreach (var child in _children)
            Window.RenderSingleElement(child);

        ImGui.EndPopup();
    }

    public override BaseUIElementData? GetNewState()
    {
        var d = (PopupData)Data;
        // Report when popup was closed by user (e.g. click-outside)
        if (!_wasOpen && d.IsOpen)
        {
            d.IsOpen = false;
            return new PopupData { Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled, IsOpen = false, Flags = d.Flags, Children = _children };
        }
        return null;
    }
}
