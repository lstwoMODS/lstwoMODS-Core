using System;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class SliderFloat2 : BaseUIElement<SliderFloat2>
{
    private Ref<Vector2>? _binding;
    public Action<Vector2>? OnValueChanged;

    public Vector2 Value
    {
        get { var d = (SliderFloat2Data)Data; return new Vector2(d.X, d.Y); }
        set { var d = (SliderFloat2Data)Data; d.X = value.x; d.Y = value.y; MarkChanged(); }
    }

    public SliderFloat2(string name, Vector2 value = default, float min = 0f, float max = 1f,
                        string format = "%.3f", Action<Vector2> onValueChanged = null,
                        ImGuiSliderFlags flags = ImGuiSliderFlags.None, bool mainThread = true) : base(name)
    {
        Data = new SliderFloat2Data { Name = name, X = value.x, Y = value.y, Min = min, Max = max, Format = format, Flags = flags };
        OnValueChanged = onValueChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public SliderFloat2 WithValue(Ref<Vector2> binding)
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
