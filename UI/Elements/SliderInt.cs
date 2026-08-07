using System;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class SliderInt : BaseUIElement<SliderInt>
{
    private Ref<int>? _binding;
    public Action<int>? OnValueChanged;

    public int Value
    {
        get => ((SliderIntData)Data).Value;
        set { ((SliderIntData)Data).Value = value; MarkChanged(); }
    }

    public SliderInt(string name, int value = 0, int min = 0, int max = 100,
                     string format = "%d", Action<int> onValueChanged = null,
                     ImGuiSliderFlags flags = ImGuiSliderFlags.None, bool mainThread = true) : base(name)
    {
        Data = new SliderIntData { Name = name, Value = value, Min = min, Max = max, Format = format, Flags = flags };
        OnValueChanged = onValueChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public SliderInt WithValue(Ref<int> binding)
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
