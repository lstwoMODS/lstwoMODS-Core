using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class CheckboxRenderer : UIRenderer
{
    private bool _value;

    public CheckboxRenderer(BaseUIElementData data) : base(data) { _value = ((CheckboxData)data).Value; }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (CheckboxData)data;
        Data = d; Name = d.Name; _value = d.Value;
    }

    public override void Render() { ImGui.Checkbox(Data.Name, ref _value); }

    public override BaseUIElementData? GetNewState()
    {
        var d = (CheckboxData)Data;
        if (_value == d.Value) return null;
        d.Value = _value;
        return new CheckboxData { Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled, Value = _value };
    }
}
