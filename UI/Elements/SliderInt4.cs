using System;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class SliderInt4 : BaseUIElement<SliderInt4>
{
    public Action<(int X, int Y, int Z, int W)>? OnValueChanged;
    private Ref<(int X, int Y, int Z, int W)>? _binding;

    public (int X, int Y, int Z, int W) Value
    {
        get { var d = (SliderInt4Data)Data; return (d.X, d.Y, d.Z, d.W); }
        set { var d = (SliderInt4Data)Data; d.X = value.X; d.Y = value.Y; d.Z = value.Z; d.W = value.W; MarkChanged(); }
    }

    public SliderInt4(string name, int x = 0, int y = 0, int z = 0, int w = 0,
                      int min = 0, int max = 100, string format = "%d",
                      Action<(int, int, int, int)> onValueChanged = null,
                      ImGuiSliderFlags flags = ImGuiSliderFlags.None, bool mainThread = true) : base(name)
    {
        Data = new SliderInt4Data { Name = name, X = x, Y = y, Z = z, W = w, Min = min, Max = max, Format = format, Flags = flags };
        OnValueChanged = onValueChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public SliderInt4 WithValue(Ref<(int X, int Y, int Z, int W)> binding)
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
