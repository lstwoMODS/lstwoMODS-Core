using System;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class DragFloat : BaseUIElement<DragFloat>
{
    private Ref<float>? _binding;
    public Action<float>? OnValueChanged;

    public float Value
    {
        get => ((DragFloatData)Data).Value;
        set
        {
            ((DragFloatData)Data).Value = value;
            MarkChanged();
        }
    }

    /// <param name="mainThread">
    /// When true (default), OnValueChanged fires on Unity's main thread via Plugin.Update().
    /// Set false to fire immediately on the IPC thread  survives game freezes but unsafe for Unity APIs.
    /// </param>
    public DragFloat(string name, float value = 0f, float speed = 1f, float min = 0f, float max = 0f,
                     string format = "%.3f", Action<float> onValueChanged = null,
                     ImGuiSliderFlags flags = ImGuiSliderFlags.None, bool mainThread = true) : base(name)
    {
        Data = new DragFloatData
        {
            Name   = name,
            Value  = value,
            Speed  = speed,
            Min    = min,
            Max    = max,
            Format = format,
            Flags  = flags
        };

        OnValueChanged           = onValueChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public DragFloat WithValue(Ref<float> binding)
    {
        _binding = binding;
        Value = binding.Value;
        binding.Changed += v => Value = v;
        return this;
    }

    public override void ApplyReceivedData(BaseUIElementData data)
    {
        var oldValue = Value;
        base.ApplyReceivedData(data);
        if (oldValue != Value)
        {
            if (_binding != null) _binding.Value = Value;
            var v = Value;
            InvokeCallback(() => OnValueChanged?.Invoke(v));
        }
    }
}
