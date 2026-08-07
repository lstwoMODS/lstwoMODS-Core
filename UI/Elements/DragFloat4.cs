using System;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class DragFloat4 : BaseUIElement<DragFloat4>
{
    private Action<Vec4>? _pushToBinding;
    public Action<Vec4>? OnValueChanged;

    public Vec4 Value
    {
        get { var d = (DragFloat4Data)Data; return new Vec4(d.X, d.Y, d.Z, d.W); }
        set { var d = (DragFloat4Data)Data; d.X = value.x; d.Y = value.y; d.Z = value.z; d.W = value.w; MarkChanged(); }
    }

    public DragFloat4(string name, Vec4 value = default, float speed = 1f, float min = 0f, float max = 0f,
                      string format = "%.3f", Action<Vec4> onValueChanged = null,
                      ImGuiSliderFlags flags = ImGuiSliderFlags.None, bool mainThread = true) : base(name)
    {
        Data = new DragFloat4Data { Name = name, X = value.x, Y = value.y, Z = value.z, W = value.w, Speed = speed, Min = min, Max = max, Format = format, Flags = flags };
        OnValueChanged = onValueChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public DragFloat4 WithValue(Ref<Vec4> binding)
    {
        _pushToBinding = v => binding.Value = v;
        Value = binding.Value;
        binding.Changed += v => Value = v;
        return this;
    }

    /// <summary>Binds a Unity-typed ref, for callers that already hold one. Prefer the
    /// <see cref="Vec4"/> overload for values that get saved: <see cref="Vector4"/> cannot be
    /// serialized.</summary>
    public DragFloat4 WithValue(Ref<Vector4> binding)
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
