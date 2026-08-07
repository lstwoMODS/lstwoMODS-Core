using System;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

/// <summary>ImGui.SliderAngle: angle in radians, displayed in degrees.</summary>
public class SliderAngle : BaseUIElement<SliderAngle>
{
    private Ref<float>? _binding;
    public Action<float>? OnValueChanged; // value in radians

    public float AngleRad
    {
        get => ((SliderAngleData)Data).AngleRad;
        set { ((SliderAngleData)Data).AngleRad = value; MarkChanged(); }
    }

    public SliderAngle(string name, float angleRad = 0f, float minDegrees = -360f, float maxDegrees = 360f,
                       string format = "%.0f deg", Action<float> onValueChanged = null,
                       ImGuiSliderFlags flags = ImGuiSliderFlags.None, bool mainThread = true) : base(name)
    {
        Data = new SliderAngleData { Name = name, AngleRad = angleRad, MinDegrees = minDegrees, MaxDegrees = maxDegrees, Format = format, Flags = flags };
        OnValueChanged = onValueChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public SliderAngle WithAngleRad(Ref<float> binding)
    {
        _binding = binding;
        AngleRad = binding.Value;
        binding.Changed += v => AngleRad = v;
        return this;
    }

    public override void ApplyReceivedData(BaseUIElementData data)
    {
        var old = AngleRad;
        base.ApplyReceivedData(data);
        if (old != AngleRad) { if (_binding != null) _binding.Value = AngleRad; var v = AngleRad; InvokeCallback(() => OnValueChanged?.Invoke(v)); }
    }
}
