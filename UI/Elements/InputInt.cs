using System;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class InputInt : BaseUIElement<InputInt>
{
    private Ref<int>? _binding;
    public Action<int>? OnValueChanged;

    public int Value
    {
        get => ((InputIntData)Data).Value;
        set { ((InputIntData)Data).Value = value; MarkChanged(); }
    }

    public InputInt(string name, int value = 0, int step = 1, int stepFast = 100,
                    Action<int> onValueChanged = null,
                    ImGuiInputTextFlags flags = ImGuiInputTextFlags.None, bool mainThread = true) : base(name)
    {
        Data = new InputIntData { Name = name, Value = value, Step = step, StepFast = stepFast, Flags = flags };
        OnValueChanged = onValueChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public InputInt WithValue(Ref<int> binding)
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
