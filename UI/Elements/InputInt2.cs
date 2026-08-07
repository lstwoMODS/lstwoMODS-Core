using System;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class InputInt2 : BaseUIElement<InputInt2>
{
    private Action<Vec2Int>? _pushToBinding;
    public Action<Vec2Int>? OnValueChanged;

    public Vec2Int Value
    {
        get { var d = (InputInt2Data)Data; return new Vec2Int(d.X, d.Y); }
        set { var d = (InputInt2Data)Data; d.X = value.x; d.Y = value.y; MarkChanged(); }
    }

    public InputInt2(string name, Vec2Int value = default,
                     Action<Vec2Int> onValueChanged = null,
                     ImGuiInputTextFlags flags = ImGuiInputTextFlags.None, bool mainThread = true) : base(name)
    {
        Data = new InputInt2Data { Name = name, X = value.x, Y = value.y, Flags = flags };
        OnValueChanged = onValueChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public InputInt2 WithValue(Ref<Vec2Int> binding)
    {
        _pushToBinding = v => binding.Value = v;
        Value = binding.Value;
        binding.Changed += v => Value = v;
        return this;
    }

    /// <summary>Binds a Unity-typed ref, for callers that already hold one. Prefer the
    /// <see cref="Vec2Int"/> overload for values that get saved: it serializes as a plain
    /// {"X":..,"Y":..} object rather than dragging Unity's derived properties along.</summary>
    public InputInt2 WithValue(Ref<Vector2Int> binding)
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
