using System;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class SearchableCombo : BaseUIElement<SearchableCombo>
{
    private Ref<int>?      _binding;
    private Ref<string[]>? _itemsBinding;
    public Action<int>?    OnChanged;

    public int SelectedIndex
    {
        get => ((SearchableComboData)Data).SelectedIndex;
        set { ((SearchableComboData)Data).SelectedIndex = value; MarkChanged(); }
    }

    /// <param name="name">Unique ID and ImGui label.</param>
    /// <param name="items">List of options shown in the dropdown.</param>
    /// <param name="selectedIndex">Initial selection (0-based).</param>
    public SearchableCombo(string name, string[] items, int selectedIndex = 0, Action<int> onChanged = null,
                           ImGuiComboFlags flags = ImGuiComboFlags.None, bool mainThread = true) : base(name)
    {
        OnChanged                = onChanged;
        RunCallbacksOnMainThread = mainThread;
        Data = new SearchableComboData
        {
            Name          = name,
            Items         = items ?? System.Array.Empty<string>(),
            SelectedIndex = selectedIndex,
            Flags         = flags
        };
    }

    public SearchableCombo WithSelectedIndex(Ref<int> binding)
    {
        _binding = binding;
        ((SearchableComboData)Data).SelectedIndex = binding.Value;
        binding.Changed += v => SelectedIndex = v;
        return this;
    }

    public SearchableCombo WithItems(Ref<string[]> binding)
    {
        _itemsBinding = binding;
        ((SearchableComboData)Data).Items = binding.Value;
        MarkChanged();
        binding.Changed += v => { ((SearchableComboData)Data).Items = v; MarkChanged(); };
        return this;
    }

    public override void ApplyReceivedData(BaseUIElementData data)
    {
        var old = SelectedIndex;
        base.ApplyReceivedData(data);
        if (old != SelectedIndex)
        {
            if (_binding != null) _binding.Value = SelectedIndex;
            var v = SelectedIndex;
            InvokeCallback(() => OnChanged?.Invoke(v));
        }
    }
}
