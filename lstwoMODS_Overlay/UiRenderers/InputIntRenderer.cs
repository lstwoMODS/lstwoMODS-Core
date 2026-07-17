using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class InputIntRenderer : UIRenderer
{
    private int _value, _step, _stepFast;
    private ImGuiInputTextFlags _flags;

    public InputIntRenderer(BaseUIElementData data) : base(data)
    {
        var d = (InputIntData)data;
        _value = d.Value;
        _step = d.Step;
        _stepFast = d.StepFast;
        _flags = (ImGuiInputTextFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (InputIntData)data;
        Data = d;
        Name = d.Name;
        _value = d.Value;
        _step = d.Step;
        _stepFast = d.StepFast;
        _flags = (ImGuiInputTextFlags)(int)d.Flags;
    }

    public override void Render() { ImGui.InputInt(Data.Name, ref _value, _step, _stepFast, _flags); }

    public override BaseUIElementData? GetNewState()
    {
        var d = (InputIntData)Data;
        if (_value == d.Value) return null;
        d.Value = _value;
        return new InputIntData { Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled, Value = _value, Step = _step, StepFast = _stepFast, Flags = d.Flags };
    }
}
