using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class RadioButtonRenderer : UIRenderer
{
    private int    _selectedValue;
    private int    _optionValue;
    private string _label;

    public RadioButtonRenderer(BaseUIElementData data) : base(data)
    {
        var d = (RadioButtonData)data;
        _selectedValue = d.SelectedValue;
        _optionValue   = d.OptionValue;
        _label         = d.Label;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (RadioButtonData)data;
        Data = d; Name = d.Name;
        _selectedValue = d.SelectedValue;
        _optionValue   = d.OptionValue;
        _label         = d.Label;
    }

    public override void Render()
    {
        // ImGui.RadioButton(string label, ref int v, int vButton)  sets v = vButton when clicked
        ImGui.RadioButton(_label, ref _selectedValue, _optionValue);
    }

    public override BaseUIElementData? GetNewState()
    {
        var d = (RadioButtonData)Data;
        if (_selectedValue == d.SelectedValue) return null;
        d.SelectedValue = _selectedValue;
        return new RadioButtonData { Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled, Label = _label, SelectedValue = _selectedValue, OptionValue = _optionValue };
    }
}
