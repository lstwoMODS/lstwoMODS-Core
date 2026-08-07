using System;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class InputFloat2 : BaseUIElement<InputFloat2>
{
    private Action<Vec2>? _pushToBinding;
    public Action<Vec2>? OnValueChanged;

    public Vec2 Value
    {
        get { var d = (InputFloat2Data)Data; return new Vec2(d.X, d.Y); }
        set { var d = (InputFloat2Data)Data; d.X = value.x; d.Y = value.y; MarkChanged(); }
    }

    public InputFloat2(string name, Vec2 value = default, string format = "%.3f",
                       Action<Vec2> onValueChanged = null,
                       ImGuiInputTextFlags flags = ImGuiInputTextFlags.None, bool mainThread = true) : base(name)
    {
        Data = new InputFloat2Data { Name = name, X = value.x, Y = value.y, Format = format, Flags = flags };
        OnValueChanged = onValueChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public InputFloat2 WithValue(Ref<Vec2> binding)
    {
        _pushToBinding = v => binding.Value = v;
        Value = binding.Value;
        binding.Changed += v => Value = v;
        return this;
    }

    /// <summary>Binds a Unity-typed ref, for callers that already hold one. Prefer the
    /// <see cref="Vec2"/> overload for values that get saved: <see cref="Vector2"/> cannot be
    /// serialized.</summary>
    public InputFloat2 WithValue(Ref<Vector2> binding)
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
