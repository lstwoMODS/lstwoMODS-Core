using System;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class DragFloat2 : BaseUIElement<DragFloat2>
{
    private Action<Vec2>? _pushToBinding;
    public Action<Vec2>? OnValueChanged;

    public Vec2 Value
    {
        get { var d = (DragFloat2Data)Data; return new Vec2(d.X, d.Y); }
        set { var d = (DragFloat2Data)Data; d.X = value.x; d.Y = value.y; MarkChanged(); }
    }

    public DragFloat2(string name, Vec2 value = default, float speed = 1f, float min = 0f, float max = 0f,
                      string format = "%.3f", Action<Vec2> onValueChanged = null,
                      ImGuiSliderFlags flags = ImGuiSliderFlags.None, bool mainThread = true) : base(name)
    {
        Data = new DragFloat2Data { Name = name, X = value.x, Y = value.y, Speed = speed, Min = min, Max = max, Format = format, Flags = flags };
        OnValueChanged = onValueChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public DragFloat2 WithValue(Ref<Vec2> binding)
    {
        _pushToBinding = v => binding.Value = v;
        Value = binding.Value;
        binding.Changed += v => Value = v;
        return this;
    }

    /// <summary>Binds a Unity-typed ref, for callers that already hold one. Prefer the
    /// <see cref="Vec2"/> overload for values that get saved: <see cref="Vector2"/> cannot be
    /// serialized.</summary>
    public DragFloat2 WithValue(Ref<Vector2> binding)
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
