using System;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class InputFloat3 : BaseUIElement<InputFloat3>
{
    private Action<Vec3>? _pushToBinding;
    public Action<Vec3>? OnValueChanged;

    public Vec3 Value
    {
        get { var d = (InputFloat3Data)Data; return new Vec3(d.X, d.Y, d.Z); }
        set { var d = (InputFloat3Data)Data; d.X = value.x; d.Y = value.y; d.Z = value.z; MarkChanged(); }
    }

    public InputFloat3(string name, Vec3 value = default, string format = "%.3f",
                       Action<Vec3> onValueChanged = null,
                       ImGuiInputTextFlags flags = ImGuiInputTextFlags.None, bool mainThread = true) : base(name)
    {
        Data = new InputFloat3Data { Name = name, X = value.x, Y = value.y, Z = value.z, Format = format, Flags = flags };
        OnValueChanged = onValueChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public InputFloat3 WithValue(Ref<Vec3> binding)
    {
        _pushToBinding = v => binding.Value = v;
        Value = binding.Value;
        binding.Changed += v => Value = v;
        return this;
    }

    /// <summary>Binds a Unity-typed ref, for callers that already hold one. Prefer the
    /// <see cref="Vec3"/> overload for values that get saved: <see cref="Vector3"/> cannot be
    /// serialized.</summary>
    public InputFloat3 WithValue(Ref<Vector3> binding)
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
