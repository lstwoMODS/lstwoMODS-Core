using System;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class DragInt4 : BaseUIElement<DragInt4>
{
    public Action<(int X, int Y, int Z, int W)>? OnValueChanged;
    private Ref<(int X, int Y, int Z, int W)>? _binding;

    public (int X, int Y, int Z, int W) Value
    {
        get { var d = (DragInt4Data)Data; return (d.X, d.Y, d.Z, d.W); }
        set { var d = (DragInt4Data)Data; d.X = value.X; d.Y = value.Y; d.Z = value.Z; d.W = value.W; MarkChanged(); }
    }

    public DragInt4(string name, int x = 0, int y = 0, int z = 0, int w = 0,
                    float speed = 1f, int min = 0, int max = 0,
                    string format = "%d", Action<(int, int, int, int)> onValueChanged = null,
                    ImGuiSliderFlags flags = ImGuiSliderFlags.None, bool mainThread = true) : base(name)
    {
        Data = new DragInt4Data { Name = name, X = x, Y = y, Z = z, W = w, Speed = speed, Min = min, Max = max, Format = format, Flags = flags };
        OnValueChanged = onValueChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public DragInt4 WithValue(Ref<(int X, int Y, int Z, int W)> binding)
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
