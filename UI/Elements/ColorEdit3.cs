using System;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

/// <summary>ImGui.ColorEdit3: RGB color editor (ignores alpha). Values are <see cref="Col"/>,
/// which converts implicitly to and from <see cref="Color"/>.</summary>
public class ColorEdit3 : BaseUIElement<ColorEdit3>
{
    private Action<Col>? _pushToBinding;
    public Action<Col>? OnChanged;

    public Col Value
    {
        get { var d = (ColorEdit3Data)Data; return new Col(d.R, d.G, d.B); }
        set { var d = (ColorEdit3Data)Data; d.R = value.r; d.G = value.g; d.B = value.b; MarkChanged(); }
    }

    public ColorEdit3(string name, Col value = default, Action<Col> onChanged = null,
                      ImGuiColorEditFlags flags = ImGuiColorEditFlags.None, bool mainThread = true) : base(name)
    {
        Data = new ColorEdit3Data { Name = name, R = value.r, G = value.g, B = value.b, Flags = flags };
        OnChanged = onChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public ColorEdit3 WithValue(Ref<Col> binding)
    {
        _pushToBinding = v => binding.Value = v;
        Value = binding.Value;
        binding.Changed += v => Value = v;
        return this;
    }

    /// <summary>Binds a Unity-typed ref, for callers that already hold one. Prefer the
    /// <see cref="Col"/> overload for values that get saved: <see cref="Color"/> cannot be
    /// serialized.</summary>
    public ColorEdit3 WithValue(Ref<Color> binding)
    {
        _pushToBinding = v => binding.Value = v;
        Value = binding.Value;
        binding.Changed += v => Value = v;
        return this;
    }

    public override void ApplyReceivedData(BaseUIElementData data)
    {
        var old = Value;
        base.ApplyReceivedData(data);
        var nv = Value;
        if (old != nv) { _pushToBinding?.Invoke(nv); var v = nv; InvokeCallback(() => OnChanged?.Invoke(v)); }
    }
}
