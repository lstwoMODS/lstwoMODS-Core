using System;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class SliderInt2 : BaseUIElement<SliderInt2>
{
    private Ref<Vector2Int>? _binding;
    public Action<Vector2Int>? OnValueChanged;

    public Vector2Int Value
    {
        get { var d = (SliderInt2Data)Data; return new Vector2Int(d.X, d.Y); }
        set { var d = (SliderInt2Data)Data; d.X = value.x; d.Y = value.y; MarkChanged(); }
    }

    public SliderInt2(string name, Vector2Int value = default, int min = 0, int max = 100,
                      string format = "%d", Action<Vector2Int> onValueChanged = null,
                      ImGuiSliderFlags flags = ImGuiSliderFlags.None, bool mainThread = true) : base(name)
    {
        Data = new SliderInt2Data { Name = name, X = value.x, Y = value.y, Min = min, Max = max, Format = format, Flags = flags };
        OnValueChanged = onValueChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public SliderInt2 WithValue(Ref<Vector2Int> binding)
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
