using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class SearchableComboRenderer : UIRenderer
{
    private string[]        _items;
    private int             _selectedIndex;
    private ImGuiComboFlags _flags;
    private ImGuiTextFilterPtr _filter;

    public SearchableComboRenderer(BaseUIElementData data) : base(data)
    {
        var d = (SearchableComboData)data;
        _items = d.Items;
        _selectedIndex = d.SelectedIndex;
        _flags = (ImGuiComboFlags)(int)d.Flags;
        _filter = ImGui.ImGuiTextFilter();
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d      = (SearchableComboData)data;
        Data           = d; Name = d.Name;
        _items         = d.Items ?? _items;
        _selectedIndex = d.SelectedIndex;
        _flags         = (ImGuiComboFlags)(int)d.Flags;
    }

    public override void Render()
    {
        if (_items == null || _items.Length == 0)
        {
            ImGui.TextDisabled($"{Data.Name} (no items)");
            return;
        }

        var previewValue = _selectedIndex >= 0 && _selectedIndex < _items.Length
            ? _items[_selectedIndex]
            : "";

        if (ImGui.BeginCombo(Data.Name, previewValue, _flags))
        {
            if (ImGui.IsWindowAppearing())
            {
                ImGui.SetKeyboardFocusHere();
                _filter.Clear();
            }
            ImGui.SetNextItemShortcut((int)(ImGuiKey.ModCtrl | ImGuiKey.F));
            _filter.Draw("##Filter", -float.Epsilon);

            for (var i = 0; i < _items.Length; i++)
            {
                if (_filter.PassFilter(_items[i]))
                {
                    var selected = i == _selectedIndex;
                    // Selectable keys its ImGui id off the item string, so duplicate item
                    // strings would share one id and route activation to the first match.
                    // PushID(i) makes every row uniquely identifiable regardless of text.
                    ImGui.PushID(i);
                    if (ImGui.Selectable(_items[i], selected))
                        _selectedIndex = i;
                    ImGui.PopID();
                }
            }
            ImGui.EndCombo();
        }
    }

    public override BaseUIElementData? GetNewState()
    {
        var d = (SearchableComboData)Data;
        if (_selectedIndex == d.SelectedIndex) return null;
        d.SelectedIndex = _selectedIndex;
        return new SearchableComboData
        {
            Id            = Data.Id,
            Name          = Data.Name,
            Enabled       = Data.Enabled,
            Items         = _items,
            SelectedIndex = _selectedIndex,
            Flags         = d.Flags
        };
    }
}
