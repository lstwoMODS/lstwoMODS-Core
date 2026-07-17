using System;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class SliderInt3 : BaseUIElement<SliderInt3>
{
    private Ref<Vector3Int>? _binding;
    public Action<Vector3Int>? OnValueChanged;

    public Vector3Int Value
    {
        get { var d = (SliderInt3Data)Data; return new Vector3Int(d.X, d.Y, d.Z); }
        set { var d = (SliderInt3Data)Data; d.X = value.x; d.Y = value.y; d.Z = value.z; MarkChanged(); }
    }

    public SliderInt3(string name, Vector3Int value = default, int min = 0, int max = 100,
                      string format = "%d", Action<Vector3Int> onValueChanged = null,
                      ImGuiSliderFlags flags = ImGuiSliderFlags.None, bool mainThread = true) : base(name)
    {
        Data = new SliderInt3Data { Name = name, X = value.x, Y = value.y, Z = value.z, Min = min, Max = max, Format = format, Flags = flags };
        OnValueChanged = onValueChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public SliderInt3 WithValue(Ref<Vector3Int> binding)
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
