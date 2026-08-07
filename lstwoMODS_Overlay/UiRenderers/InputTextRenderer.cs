using System.Collections.Generic;
using System.Numerics;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class InputTextRenderer : UIRenderer
{
    private string _value;
    private string _hint;
    private int _maxLength;
    private bool _multiline;
    private float _sizeX, _sizeY;
    private ImGuiInputTextFlags _flags;

    private List<int> _watchKeys = new();
    private int _lastKeyPressed;
    private int _lastKeyVersion;
    private int _appliedFocusVersion;
    private bool _isFocused;
    private bool _wasActive;
    private bool _lastFocusedSent;
    private bool _reloadBuf;

    public InputTextRenderer(BaseUIElementData data) : base(data)
    {
        var d = (InputTextData)data;
        _value = d.Value;
        _hint = d.Hint;
        _maxLength = d.MaxLength;
        _multiline = d.Multiline;
        _sizeX = d.SizeX;
        _sizeY = d.SizeY;
        _flags = (ImGuiInputTextFlags)(int)d.Flags;
        _watchKeys = d.WatchKeys ?? new List<int>();
        _lastKeyVersion = d.LastKeyVersion;

        if (d.RequestFocus && d.RequestFocusVersion > 0)
        {
            _pendingFocus = true;
            _appliedFocusVersion = d.RequestFocusVersion;
        }
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (InputTextData)data;
        Data = d;
        Name = d.Name;
        if (d.Value != _value)
            _reloadBuf = true;
        _value = d.Value;
        _hint = d.Hint;
        _maxLength = d.MaxLength;
        _multiline = d.Multiline;
        _sizeX = d.SizeX;
        _sizeY = d.SizeY;
        _flags = (ImGuiInputTextFlags)(int)d.Flags;
        _watchKeys = d.WatchKeys ?? _watchKeys;

        if (d.RequestFocus && d.RequestFocusVersion != _appliedFocusVersion)
        {
            _pendingFocus = true;
            _appliedFocusVersion = d.RequestFocusVersion;
        }
    }

    private bool _pendingFocus;

    public override void Render()
    {
        if (_reloadBuf)
        {
            if (_wasActive)
            {
                ImGuiP.ClearActiveID();
                _pendingFocus = true;
            }
            _reloadBuf = false;
        }

        if (_pendingFocus)
            ImGui.SetKeyboardFocusHere();

        bool submitted;
        if (_multiline)
            submitted = ImGui.InputTextMultiline(Data.Name, ref _value, (UIntPtr)_maxLength, new Vector2(_sizeX, _sizeY), _flags);
        else if (!string.IsNullOrEmpty(_hint))
            submitted = ImGui.InputTextWithHint(Data.Name, _hint, ref _value, (UIntPtr)_maxLength, _flags);
        else
            submitted = ImGui.InputText(Data.Name, ref _value, (UIntPtr)_maxLength, _flags);

        var active      = ImGui.IsItemActive();
        var deactivated = ImGui.IsItemDeactivated();
        _isFocused = active;

        if (active && _watchKeys.Count > 0)
        {
            foreach (var k in _watchKeys)
            {
                var wk = (ImGuiKey)k;
                if (wk == ImGuiKey.UpArrow || wk == ImGuiKey.DownArrow ||
                    wk == ImGuiKey.LeftArrow || wk == ImGuiKey.RightArrow)
                    ImGuiP.SetItemKeyOwner(wk, ImGuiInputFlags.None);
            }
        }

        if (_pendingFocus && active)
            _pendingFocus = false;

        var pathA = submitted && (_flags & ImGuiInputTextFlags.EnterReturnsTrue) != 0 && _wasActive;
        if (pathA)
        {
            _lastKeyPressed = (int)ImGuiKey.Enter;
            _lastKeyVersion++;
        }

        var pathB = false;
        
        if ((active || deactivated) && _watchKeys.Count > 0)
        {
            foreach (var k in _watchKeys)
            {
                if (ImGui.IsKeyPressed((ImGuiKey)k, false))
                {
                    _lastKeyPressed = k;
                    _lastKeyVersion++;
                }
            }
        }

        _wasActive = active;
    }

    public override BaseUIElementData? GetNewState()
    {
        var d = (InputTextData)Data;
        var valueChanged = _value != d.Value;
        var keyChanged   = _lastKeyVersion != d.LastKeyVersion;
        var focusChanged = _isFocused != _lastFocusedSent;
        if (!valueChanged && !keyChanged && !focusChanged) return null;

        _lastFocusedSent = _isFocused;
        d.Value          = _value;
        d.LastKeyPressed = _lastKeyPressed;
        d.LastKeyVersion = _lastKeyVersion;
        d.IsFocused      = _isFocused;

        return new InputTextData
        {
            Id = Data.Id,
            Name = Data.Name,
            Enabled = Data.Enabled,
            Value = _value,
            Hint = _hint,
            MaxLength = _maxLength,
            Multiline = _multiline,
            SizeX = _sizeX,
            SizeY = _sizeY,
            Flags = d.Flags,
            WatchKeys = _watchKeys,
            LastKeyPressed = _lastKeyPressed,
            LastKeyVersion = _lastKeyVersion,
            IsFocused = _isFocused,
        };
    }
}
