using System;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class RadioButton : BaseUIElement<RadioButton>
{
    private readonly Ref<int> _group;
    public Action<int>? OnChanged;

    public RadioButton(string name, string label, Ref<int> group, int optionValue, Action<int> onChanged = null, bool mainThread = true) : base(name)
    {
        _group    = group;
        OnChanged = onChanged;
        RunCallbacksOnMainThread = mainThread;

        Data = new RadioButtonData
        {
            Name          = name,
            Label         = label,
            SelectedValue = group?.Value ?? 0,
            OptionValue   = optionValue,
        };

        if (group != null) group.Changed += v => { ((RadioButtonData)Data).SelectedValue = v; MarkChanged(); };
    }

    public override void ApplyReceivedData(BaseUIElementData data)
    {
        var old = ((RadioButtonData)Data).SelectedValue;
        base.ApplyReceivedData(data);
        var newVal = ((RadioButtonData)Data).SelectedValue;
        if (old != newVal)
        {
            _group.Value = newVal;
            InvokeCallback(() => OnChanged?.Invoke(newVal));
        }
    }
}
