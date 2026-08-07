using System;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class DragInt : BaseUIElement<DragInt>
{
    private Ref<int>? _binding;
    public Action<int>? OnValueChanged;

    public int Value
    {
        get => ((DragIntData)Data).Value;
        set { ((DragIntData)Data).Value = value; MarkChanged(); }
    }

    public DragInt(string name, int value = 0, float speed = 1f, int min = 0, int max = 0,
                   string format = "%d", Action<int> onValueChanged = null,
                   ImGuiSliderFlags flags = ImGuiSliderFlags.None, bool mainThread = true) : base(name)
    {
        Data = new DragIntData { Name = name, Value = value, Speed = speed, Min = min, Max = max, Format = format, Flags = flags };
        OnValueChanged = onValueChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public DragInt WithValue(Ref<int> binding)
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
