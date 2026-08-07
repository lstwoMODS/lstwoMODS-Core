using System;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

/// <summary>ImGui.DragInt4: draggable four-component integer. Values are <see cref="Vec4Int"/>,
/// which converts implicitly to and from a <c>(x, y, z, w)</c> tuple. Unity has no
/// <c>Vector4Int</c>, so there is no Unity-typed overload here.</summary>
public class DragInt4 : BaseUIElement<DragInt4>
{
    public Action<Vec4Int>? OnValueChanged;
    private Action<Vec4Int>? _pushToBinding;

    public Vec4Int Value
    {
        get { var d = (DragInt4Data)Data; return new Vec4Int(d.X, d.Y, d.Z, d.W); }
        set { var d = (DragInt4Data)Data; d.X = value.X; d.Y = value.Y; d.Z = value.Z; d.W = value.W; MarkChanged(); }
    }

    public DragInt4(string name, int x = 0, int y = 0, int z = 0, int w = 0,
                    float speed = 1f, int min = 0, int max = 0,
                    string format = "%d", Action<Vec4Int> onValueChanged = null,
                    ImGuiSliderFlags flags = ImGuiSliderFlags.None, bool mainThread = true) : base(name)
    {
        Data = new DragInt4Data { Name = name, X = x, Y = y, Z = z, W = w, Speed = speed, Min = min, Max = max, Format = format, Flags = flags };
        OnValueChanged = onValueChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public DragInt4 WithValue(Ref<Vec4Int> binding)
    {
        _pushToBinding = v => binding.Value = v;
        Value = binding.Value;
        binding.Changed += v => Value = v;
        return this;
    }

    /// <summary>Binds a tuple-typed ref, for callers that already hold one. Prefer the
    /// <see cref="Vec4Int"/> overload for values that get saved.</summary>
    public DragInt4 WithValue(Ref<(int X, int Y, int Z, int W)> binding)
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
