using System;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

/// <summary>ImGui.ColorEdit3: RGB color editor. Uses UnityEngine.Color (ignores alpha).</summary>
public class ColorEdit3 : BaseUIElement<ColorEdit3>
{
    private Ref<Color>? _binding;
    public Action<Color>? OnChanged;

    public Color Value
    {
        get { var d = (ColorEdit3Data)Data; return new Color(d.R, d.G, d.B); }
        set { var d = (ColorEdit3Data)Data; d.R = value.r; d.G = value.g; d.B = value.b; MarkChanged(); }
    }

    public ColorEdit3(string name, Color value = default, Action<Color> onChanged = null,
                      ImGuiColorEditFlags flags = ImGuiColorEditFlags.None, bool mainThread = true) : base(name)
    {
        Data = new ColorEdit3Data { Name = name, R = value.r, G = value.g, B = value.b, Flags = flags };
        OnChanged = onChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public ColorEdit3 WithValue(Ref<Color> binding)
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
