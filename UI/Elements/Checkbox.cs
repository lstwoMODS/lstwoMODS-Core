using System;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class Checkbox : BaseUIElement<Checkbox>
{
    private Ref<bool>? _binding;
    public Action<bool>? OnChanged;

    public bool Value
    {
        get => ((CheckboxData)Data).Value;
        set { ((CheckboxData)Data).Value = value; MarkChanged(); }
    }

    public Checkbox(string name, bool value = false, Action<bool> onChanged = null, bool mainThread = true) : base(name)
    {
        Data = new CheckboxData { Name = name, Value = value };
        OnChanged = onChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public Checkbox WithValue(Ref<bool> binding)
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
        if (old != Value)
        {
            if (_binding != null) _binding.Value = Value;
            var v = Value;
            InvokeCallback(() => OnChanged?.Invoke(v));
        }
    }
}
