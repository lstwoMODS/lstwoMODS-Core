using System;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class InputFloat3 : BaseUIElement<InputFloat3>
{
    private Ref<Vector3>? _binding;
    public Action<Vector3>? OnValueChanged;

    public Vector3 Value
    {
        get { var d = (InputFloat3Data)Data; return new Vector3(d.X, d.Y, d.Z); }
        set { var d = (InputFloat3Data)Data; d.X = value.x; d.Y = value.y; d.Z = value.z; MarkChanged(); }
    }

    public InputFloat3(string name, Vector3 value = default, string format = "%.3f",
                       Action<Vector3> onValueChanged = null,
                       ImGuiInputTextFlags flags = ImGuiInputTextFlags.None, bool mainThread = true) : base(name)
    {
        Data = new InputFloat3Data { Name = name, X = value.x, Y = value.y, Z = value.z, Format = format, Flags = flags };
        OnValueChanged = onValueChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public InputFloat3 WithValue(Ref<Vector3> binding)
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
