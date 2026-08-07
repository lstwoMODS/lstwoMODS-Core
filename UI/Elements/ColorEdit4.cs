using System;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

/// <summary>ImGui.ColorEdit4: RGBA color editor. Values are <see cref="Col"/>, which converts
/// implicitly to and from <see cref="Color"/>.</summary>
public class ColorEdit4 : BaseUIElement<ColorEdit4>
{
    private Action<Col>? _pushToBinding;
    public Action<Col>? OnChanged;

    public Col Value
    {
        get { var d = (ColorEdit4Data)Data; return new Col(d.R, d.G, d.B, d.A); }
        set { var d = (ColorEdit4Data)Data; d.R = value.r; d.G = value.g; d.B = value.b; d.A = value.a; MarkChanged(); }
    }

    public ColorEdit4(string name, Col value = default, Action<Col> onChanged = null,
                      ImGuiColorEditFlags flags = ImGuiColorEditFlags.None, bool mainThread = true) : base(name)
    {
        Data = new ColorEdit4Data { Name = name, R = value.r, G = value.g, B = value.b, A = value.a, Flags = flags };
        OnChanged = onChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public ColorEdit4 WithValue(Ref<Col> binding)
    {
        _pushToBinding = v => binding.Value = v;
        Value = binding.Value;
        binding.Changed += v => Value = v;
        return this;
    }

    /// <summary>Binds a Unity-typed ref, for callers that already hold one. Prefer the
    /// <see cref="Col"/> overload for values that get saved: <see cref="Color"/> cannot be
    /// serialized.</summary>
    public ColorEdit4 WithValue(Ref<Color> binding)
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
