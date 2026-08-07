using System;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class SliderInt3 : BaseUIElement<SliderInt3>
{
    private Action<Vec3Int>? _pushToBinding;
    public Action<Vec3Int>? OnValueChanged;

    public Vec3Int Value
    {
        get { var d = (SliderInt3Data)Data; return new Vec3Int(d.X, d.Y, d.Z); }
        set { var d = (SliderInt3Data)Data; d.X = value.x; d.Y = value.y; d.Z = value.z; MarkChanged(); }
    }

    public SliderInt3(string name, Vec3Int value = default, int min = 0, int max = 100,
                      string format = "%d", Action<Vec3Int> onValueChanged = null,
                      ImGuiSliderFlags flags = ImGuiSliderFlags.None, bool mainThread = true) : base(name)
    {
        Data = new SliderInt3Data { Name = name, X = value.x, Y = value.y, Z = value.z, Min = min, Max = max, Format = format, Flags = flags };
        OnValueChanged = onValueChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public SliderInt3 WithValue(Ref<Vec3Int> binding)
    {
        _pushToBinding = v => binding.Value = v;
        Value = binding.Value;
        binding.Changed += v => Value = v;
        return this;
    }

    /// <summary>Binds a Unity-typed ref, for callers that already hold one. Prefer the
    /// <see cref="Vec3Int"/> overload for values that get saved: it serializes as a plain
    /// {"X":..,"Y":..,"Z":..} object rather than dragging Unity's derived properties along.</summary>
    public SliderInt3 WithValue(Ref<Vector3Int> binding)
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
        if (old != nv) { _pushToBinding?.Invoke(nv); var v = nv; InvokeCallback(() => OnValueChanged?.Invoke(v)); }
    }
}
