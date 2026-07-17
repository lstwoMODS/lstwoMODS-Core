using System;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class InputInt3 : BaseUIElement<InputInt3>
{
    private Ref<Vector3Int>? _binding;
    public Action<Vector3Int>? OnValueChanged;

    public Vector3Int Value
    {
        get { var d = (InputInt3Data)Data; return new Vector3Int(d.X, d.Y, d.Z); }
        set { var d = (InputInt3Data)Data; d.X = value.x; d.Y = value.y; d.Z = value.z; MarkChanged(); }
    }

    public InputInt3(string name, Vector3Int value = default,
                     Action<Vector3Int> onValueChanged = null,
                     ImGuiInputTextFlags flags = ImGuiInputTextFlags.None, bool mainThread = true) : base(name)
    {
        Data = new InputInt3Data { Name = name, X = value.x, Y = value.y, Z = value.z, Flags = flags };
        OnValueChanged = onValueChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public InputInt3 WithValue(Ref<Vector3Int> binding)
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
