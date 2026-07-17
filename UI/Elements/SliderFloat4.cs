using System;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class SliderFloat4 : BaseUIElement<SliderFloat4>
{
    private Ref<Vector4>? _binding;
    public Action<Vector4>? OnValueChanged;

    public Vector4 Value
    {
        get { var d = (SliderFloat4Data)Data; return new Vector4(d.X, d.Y, d.Z, d.W); }
        set { var d = (SliderFloat4Data)Data; d.X = value.x; d.Y = value.y; d.Z = value.z; d.W = value.w; MarkChanged(); }
    }

    public SliderFloat4(string name, Vector4 value = default, float min = 0f, float max = 1f,
                        string format = "%.3f", Action<Vector4> onValueChanged = null,
                        ImGuiSliderFlags flags = ImGuiSliderFlags.None, bool mainThread = true) : base(name)
    {
        Data = new SliderFloat4Data { Name = name, X = value.x, Y = value.y, Z = value.z, W = value.w, Min = min, Max = max, Format = format, Flags = flags };
        OnValueChanged = onValueChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public SliderFloat4 WithValue(Ref<Vector4> binding)
    {
        _binding = binding;
        Value = binding.Value;
        binding.Changed += v => Value = v;
        return this;
    }

    public override void ApplyReceivedData(BaseUIElementData data)
    {
        var old = Value;
        base.ApplyReceivedData(data);
        var nv = Value;
        if (old != nv) { if (_binding != null) _binding.Value = nv; var v = nv; InvokeCallback(() => OnValueChanged?.Invoke(v)); }
    }
}
