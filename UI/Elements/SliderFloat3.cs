using System;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class SliderFloat3 : BaseUIElement<SliderFloat3>
{
    private Action<Vec3>? _pushToBinding;
    public Action<Vec3>? OnValueChanged;

    public Vec3 Value
    {
        get { var d = (SliderFloat3Data)Data; return new Vec3(d.X, d.Y, d.Z); }
        set { var d = (SliderFloat3Data)Data; d.X = value.x; d.Y = value.y; d.Z = value.z; MarkChanged(); }
    }

    public SliderFloat3(string name, Vec3 value = default, float min = 0f, float max = 1f,
                        string format = "%.3f", Action<Vec3> onValueChanged = null,
                        ImGuiSliderFlags flags = ImGuiSliderFlags.None, bool mainThread = true) : base(name)
    {
        Data = new SliderFloat3Data { Name = name, X = value.x, Y = value.y, Z = value.z, Min = min, Max = max, Format = format, Flags = flags };
        OnValueChanged = onValueChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public SliderFloat3 WithValue(Ref<Vec3> binding)
    {
        _pushToBinding = v => binding.Value = v;
        Value = binding.Value;
        binding.Changed += v => Value = v;
        return this;
    }

    /// <summary>Binds a Unity-typed ref, for callers that already hold one. Prefer the
    /// <see cref="Vec3"/> overload for values that get saved: <see cref="Vector3"/> cannot be
    /// serialized.</summary>
    public SliderFloat3 WithValue(Ref<Vector3> binding)
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
