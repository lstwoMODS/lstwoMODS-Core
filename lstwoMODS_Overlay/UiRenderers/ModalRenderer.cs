using System.Collections.Generic;
using System.Numerics;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class ModalRenderer : UIRenderer
{
    private string _label;
    private bool   _pendingOpen;
    private bool   _pendingClose;
    private bool   _isOpen;
    private bool   _hasClose;
    private ImGuiWindowFlags _flags;
    private List<BaseUIElementData> _children;

    public ModalRenderer(BaseUIElementData data) : base(data)
    {
        var d = (ModalData)data;
        _label    = d.Label; _hasClose = d.HasClose;
        _flags    = (ImGuiWindowFlags)(int)d.Flags;
        _children = d.Children;
        if (d.IsOpen) _pendingOpen = true;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (ModalData)data;
        var wasOpen = _isOpen;
        Data = d; Name = d.Name;
        _label = d.Label; _hasClose = d.HasClose;
        _flags = (ImGuiWindowFlags)(int)d.Flags;
        if (d.Children?.Count > 0) _children = d.Children;
        if (d.IsOpen && !wasOpen) { _pendingOpen = true; _pendingClose = false; }
        if (!d.IsOpen && wasOpen) { _pendingClose = true; _pendingOpen = false; }
    }

    // A closed modal draws nothing; its subtree (e.g. the name input) must not count toward
    // input capture, or a mounted-but-closed modal keeps the overlay wanting input.
    public override bool ParticipatesInInput => _isOpen;

    public override void Render()
    {
        if (_pendingOpen)
        {
            ImGui.OpenPopup(_label);
            _pendingOpen = false;
        }

        var d = (ModalData)Data;
        if (d.SizeX > 0 || d.SizeY > 0)
            ImGui.SetNextWindowSize(new Vector2(d.SizeX, d.SizeY), ImGuiCond.FirstUseEver);

        bool visible;
        var closeClicked = false;
        if (_hasClose)
        {
            var open = true;
            visible = ImGui.BeginPopupModal(_label, ref open, _flags);
            closeClicked = !open;
        }
        else
        {
            visible = ImGui.BeginPopupModal(_label, _flags);
        }

        if (!visible)
        {
            // Deliberately NOT reading closeClicked here: the ref-open overload also
            // reports open=false on the frame the popup just closed; honoring it would
            // re-arm _pendingClose and instantly kill the next Open() request.
            _isOpen = false;
            return;
        }

        // The ref-open overload reports open=false on the popup's first visible frame
        // even though the X was never clicked (Hexa.NET marshalling quirk). Only honor
        // the X-button state once the modal has been visibly open for a full frame.
        if (closeClicked && _isOpen) _pendingClose = true;

        if (_pendingClose) { ImGui.CloseCurrentPopup(); _pendingClose = false; }
        _isOpen = true;

        foreach (var child in _children)
            Window.RenderSingleElement(child);

        ImGui.EndPopup();
    }

    public override BaseUIElementData? GetNewState()
    {
        var d = (ModalData)Data;
        // _pendingOpen: an open request was applied but hasn't rendered yet 
        // don't report it back as "closed" or the request would cancel itself.
        if (!_isOpen && !_pendingOpen && d.IsOpen)
        {
            d.IsOpen = false;
            return new ModalData { Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled, Label = _label, IsOpen = false, HasClose = _hasClose, Flags = d.Flags, Children = _children };
        }
        return null;
    }
}
