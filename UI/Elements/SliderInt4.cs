using System;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

/// <summary>ImGui.SliderInt4: four-component integer slider. Values are <see cref="Vec4Int"/>,
/// which converts implicitly to and from a <c>(x, y, z, w)</c> tuple. Unity has no
/// <c>Vector4Int</c>, so there is no Unity-typed overload here.</summary>
public class SliderInt4 : BaseUIElement<SliderInt4>
{
    public Action<Vec4Int>? OnValueChanged;
    private Action<Vec4Int>? _pushToBinding;

    public Vec4Int Value
    {
        get { var d = (SliderInt4Data)Data; return new Vec4Int(d.X, d.Y, d.Z, d.W); }
        set { var d = (SliderInt4Data)Data; d.X = value.X; d.Y = value.Y; d.Z = value.Z; d.W = value.W; MarkChanged(); }
    }

    public SliderInt4(string name, int x = 0, int y = 0, int z = 0, int w = 0,
                      int min = 0, int max = 100, string format = "%d",
                      Action<Vec4Int> onValueChanged = null,
                      ImGuiSliderFlags flags = ImGuiSliderFlags.None, bool mainThread = true) : base(name)
    {
        Data = new SliderInt4Data { Name = name, X = x, Y = y, Z = z, W = w, Min = min, Max = max, Format = format, Flags = flags };
        OnValueChanged = onValueChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public SliderInt4 WithValue(Ref<Vec4Int> binding)
    {
        _pushToBinding = v => binding.Value = v;
        Value = binding.Value;
        binding.Changed += v => Value = v;
        return this;
    }

    /// <summary>Binds a tuple-typed ref, for callers that already hold one. Prefer the
    /// <see cref="Vec4Int"/> overload for values that get saved.</summary>
    public SliderInt4 WithValue(Ref<(int X, int Y, int Z, int W)> binding)
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
