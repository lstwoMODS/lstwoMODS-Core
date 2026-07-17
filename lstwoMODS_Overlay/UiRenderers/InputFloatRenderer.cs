using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class InputFloatRenderer : UIRenderer
{
    private float _value, _step, _stepFast;
    private string _format;
    private ImGuiInputTextFlags _flags;

    public InputFloatRenderer(BaseUIElementData data) : base(data)
    {
        var d = (InputFloatData)data;
        _value = d.Value;
        _step = d.Step;
        _stepFast = d.StepFast;
        _format = d.Format;
        _flags = (ImGuiInputTextFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (InputFloatData)data;
        Data = d;
        Name = d.Name;
        _value = d.Value;
        _step = d.Step;
        _stepFast = d.StepFast;
        _format = d.Format;
        _flags = (ImGuiInputTextFlags)(int)d.Flags;
    }

    public override void Render() { ImGui.InputFloat(Data.Name, ref _value, _step, _stepFast, _format, _flags); }

    public override BaseUIElementData? GetNewState()
    {
        var d = (InputFloatData)Data;
        if (_value == d.Value) return null;
        d.Value = _value;
        return new InputFloatData { Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled, Value = _value, Step = _step, StepFast = _stepFast, Format = _format, Flags = d.Flags };
    }
}
