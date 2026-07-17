using System;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

/// <summary>ImGui.ColorEdit4  RGBA color editor. Uses UnityEngine.Color.</summary>
public class ColorEdit4 : BaseUIElement<ColorEdit4>
{
    private Ref<Color>? _binding;
    public Action<Color>? OnChanged;

    public Color Value
    {
        get { var d = (ColorEdit4Data)Data; return new Color(d.R, d.G, d.B, d.A); }
        set { var d = (ColorEdit4Data)Data; d.R = value.r; d.G = value.g; d.B = value.b; d.A = value.a; MarkChanged(); }
    }

    public ColorEdit4(string name, Color value = default, Action<Color> onChanged = null,
                      ImGuiColorEditFlags flags = ImGuiColorEditFlags.None, bool mainThread = true) : base(name)
    {
        Data = new ColorEdit4Data { Name = name, R = value.r, G = value.g, B = value.b, A = value.a, Flags = flags };
        OnChanged = onChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public ColorEdit4 WithValue(Ref<Color> binding)
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
        if (old != nv) { if (_binding != null) _binding.Value = nv; var v = nv; InvokeCallback(() => OnChanged?.Invoke(v)); }
    }
}
