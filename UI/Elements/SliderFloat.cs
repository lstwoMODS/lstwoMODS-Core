using System;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class SliderFloat : BaseUIElement<SliderFloat>
{
    private Ref<float>? _binding;
    public Action<float>? OnValueChanged;

    public float Value
    {
        get => ((SliderFloatData)Data).Value;
        set { ((SliderFloatData)Data).Value = value; MarkChanged(); }
    }

    public SliderFloat(string name, float value = 0f, float min = 0f, float max = 1f,
                       string format = "%.3f", Action<float> onValueChanged = null,
                       ImGuiSliderFlags flags = ImGuiSliderFlags.None, bool mainThread = true) : base(name)
    {
        Data = new SliderFloatData { Name = name, Value = value, Min = min, Max = max, Format = format, Flags = flags };
        OnValueChanged = onValueChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public SliderFloat WithValue(Ref<float> binding)
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
        if (old != Value) { if (_binding != null) _binding.Value = Value; var v = Value; InvokeCallback(() => OnValueChanged?.Invoke(v)); }
    }
}
