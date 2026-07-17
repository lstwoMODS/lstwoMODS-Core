using System;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class Combo : BaseUIElement<Combo>
{
    private Ref<int>? _binding;
    private Ref<string[]>? _itemsBinding;
    public Action<int>? OnChanged;

    public int SelectedIndex
    {
        get => ((ComboData)Data).SelectedIndex;
        set { ((ComboData)Data).SelectedIndex = value; MarkChanged(); }
    }

    /// <param name="name">Unique ID and ImGui label.</param>
    /// <param name="items">List of options shown in the dropdown.</param>
    /// <param name="selectedIndex">Initial selection (0-based).</param>
    public Combo(string name, string[] items, int selectedIndex = 0, Action<int> onChanged = null,
                 ImGuiComboFlags flags = ImGuiComboFlags.None, bool mainThread = true) : base(name)
    {
        OnChanged = onChanged;
        RunCallbacksOnMainThread = mainThread;
        Data = new ComboData { Name = name, Items = items ?? System.Array.Empty<string>(), SelectedIndex = selectedIndex, Flags = flags };
    }

    public Combo WithSelectedIndex(Ref<int> binding)
    {
        _binding = binding;
        ((ComboData)Data).SelectedIndex = binding.Value;
        binding.Changed += v => SelectedIndex = v;
        return this;
    }

    public Combo WithItems(Ref<string[]> binding)
    {
        _itemsBinding = binding;
        ((ComboData)Data).Items = binding.Value;
        MarkChanged();
        binding.Changed += v => { ((ComboData)Data).Items = v; MarkChanged(); };
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
