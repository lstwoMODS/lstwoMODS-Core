using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class ComboRenderer : UIRenderer
{
    private string[]        _items;
    private int             _selectedIndex;
    private ImGuiComboFlags _flags;

    public ComboRenderer(BaseUIElementData data) : base(data)
    {
        var d = (ComboData)data;
        _items         = d.Items;
        _selectedIndex = d.SelectedIndex;
        _flags         = (ImGuiComboFlags)(int)d.Flags;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (ComboData)data;
        Data = d; Name = d.Name;
        _items         = d.Items ?? _items;
        _selectedIndex = d.SelectedIndex;
        _flags         = (ImGuiComboFlags)(int)d.Flags;
    }

    public override void Render()
    {
        if (_items == null)
        {
            ImGui.TextDisabled($"{Data.Name} (no items)");
            return;
        }
        ImGui.Combo(Data.Name, ref _selectedIndex, _items, _items.Length);
    }

    public override BaseUIElementData? GetNewState()
    {
        var d = (ComboData)Data;
        if (_selectedIndex == d.SelectedIndex) return null;
        d.SelectedIndex = _selectedIndex;
        return new ComboData { Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled, Items = _items, SelectedIndex = _selectedIndex, Flags = d.Flags };
    }
}
