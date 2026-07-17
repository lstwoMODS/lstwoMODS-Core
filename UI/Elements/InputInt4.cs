using System;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class InputInt4 : BaseUIElement<InputInt4>
{
    public Action<(int X, int Y, int Z, int W)>? OnValueChanged;
    private Ref<(int X, int Y, int Z, int W)>? _binding;

    public (int X, int Y, int Z, int W) Value
    {
        get { var d = (InputInt4Data)Data; return (d.X, d.Y, d.Z, d.W); }
        set { var d = (InputInt4Data)Data; d.X = value.X; d.Y = value.Y; d.Z = value.Z; d.W = value.W; MarkChanged(); }
    }

    public InputInt4(string name, int x = 0, int y = 0, int z = 0, int w = 0,
                     Action<(int, int, int, int)> onValueChanged = null,
                     ImGuiInputTextFlags flags = ImGuiInputTextFlags.None, bool mainThread = true) : base(name)
    {
        Data = new InputInt4Data { Name = name, X = x, Y = y, Z = z, W = w, Flags = flags };
        OnValueChanged = onValueChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public InputInt4 WithValue(Ref<(int X, int Y, int Z, int W)> binding)
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
