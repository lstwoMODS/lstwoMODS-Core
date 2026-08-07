using System;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class InputFloat : BaseUIElement<InputFloat>
{
    private Ref<float>? _binding;
    public Action<float>? OnValueChanged;

    public float Value
    {
        get => ((InputFloatData)Data).Value;
        set { ((InputFloatData)Data).Value = value; MarkChanged(); }
    }

    public InputFloat(string name, float value = 0f, float step = 0f, float stepFast = 0f,
                      string format = "%.3f", Action<float> onValueChanged = null,
                      ImGuiInputTextFlags flags = ImGuiInputTextFlags.None, bool mainThread = true) : base(name)
    {
        Data = new InputFloatData { Name = name, Value = value, Step = step, StepFast = stepFast, Format = format, Flags = flags };
        OnValueChanged = onValueChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public InputFloat WithValue(Ref<float> binding)
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
