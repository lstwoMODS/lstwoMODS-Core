using System;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class Selectable : BaseUIElement<Selectable>
{
    private Ref<bool>? _binding;
    public Action<bool>? OnChanged;

    public bool Selected
    {
        get => ((SelectableData)Data).Selected;
        set { ((SelectableData)Data).Selected = value; MarkChanged(); }
    }

    public Selectable(string name, bool selected = false, Action<bool> onChanged = null,
                      ImGuiSelectableFlags flags = ImGuiSelectableFlags.None,
                      float sizeX = 0f, float sizeY = 0f, bool mainThread = true) : base(name)
    {
        OnChanged = onChanged;
        RunCallbacksOnMainThread = mainThread;
        Data = new SelectableData { Name = name, Selected = selected, Flags = flags, SizeX = sizeX, SizeY = sizeY };
    }

    public Selectable WithSelected(Ref<bool> binding)
    {
        _binding = binding;
        Selected = binding.Value;
        binding.Changed += v => Selected = v;
        return this;
    }

    public override void ApplyReceivedData(BaseUIElementData data)
    {
        var old = Selected;
        base.ApplyReceivedData(data);
        if (old != Selected)
        {
            if (_binding != null) _binding.Value = Selected;
            var v = Selected;
            InvokeCallback(() => OnChanged?.Invoke(v));
        }
    }
}
