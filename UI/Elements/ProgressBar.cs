using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class ProgressBar : BaseUIElement<ProgressBar>
{
    private Ref<float>? _binding;

    public float Value
    {
        get => ((ProgressBarData)Data).Value;
        set { ((ProgressBarData)Data).Value = value; MarkChanged(); }
    }

    /// <param name="sizeX">Width. -1 = fill available, 0 = use ItemWidth.</param>
    /// <param name="sizeY">Height. 0 = default row height.</param>
    /// <param name="overlay">Text drawn on top of bar. null = default percentage.</param>
    public ProgressBar(string name, float value = 0f, float sizeX = -1f, float sizeY = 0f, string overlay = null) : base(name)
    {
        Data = new ProgressBarData { Name = name, Value = value, SizeX = sizeX, SizeY = sizeY, Overlay = overlay };
    }

    public ProgressBar WithValue(Ref<float> binding)
    {
        _binding = binding;
        ((ProgressBarData)Data).Value = binding.Value;
        binding.Changed += v => { ((ProgressBarData)Data).Value = v; MarkChanged(); };
        return this;
    }

    /// <summary>Bind the overlay text to a <see cref="Ref{T}"/>. null = default percentage display.</summary>
    public ProgressBar WithOverlay(Ref<string> binding)
    {
        ((ProgressBarData)Data).Overlay = binding.Value;
        binding.Changed += v => { ((ProgressBarData)Data).Overlay = v; MarkChanged(); };
        return this;
    }
}
