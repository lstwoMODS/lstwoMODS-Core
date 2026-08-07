using lstwoMODS.ImGui.Shared.UI;
using lstwoMODS_Core.UI;
using UnityEngine;

namespace lstwoMODS_Core.UI.Elements;

/// <summary>ImGui.Text  plain text label.</summary>
public class UIText : BaseUIElement<UIText>
{
    private Ref<string>? _binding;

    public string Text
    {
        get => ((TextData)Data).Text;
        set { ((TextData)Data).Text = value; MarkChanged(); }
    }

    public UIText(string name, string text) : base(name)
    {
        Data = new TextData { Name = name, Text = text, Variant = TextType.Text };
    }

    public UIText WithText(Ref<string> binding)
    {
        _binding = binding;
        Text = binding.Value ?? "";
        binding.Changed += v => Text = v ?? "";
        return this;
    }
}

/// <summary>ImGui.TextColored  colored text.</summary>
public class TextColored : BaseUIElement<TextColored>
{
    private Ref<string>? _binding;

    public TextColored(string name, string text, Col color) : base(name)
    {
        Data = new TextData { Name = name, Text = text, Variant = TextType.TextColored, R = color.r, G = color.g, B = color.b, A = color.a };
    }
    public TextColored(string name, string text, float r, float g, float b, float a = 1f) : base(name)
    {
        Data = new TextData { Name = name, Text = text, Variant = TextType.TextColored, R = r, G = g, B = b, A = a };
    }

    public TextColored WithText(Ref<string> binding)
    {
        _binding = binding;
        ((TextData)Data).Text = binding.Value ?? "";
        binding.Changed += v => { ((TextData)Data).Text = v ?? ""; MarkChanged(); };
        return this;
    }
}

/// <summary>ImGui.TextDisabled  dimmed/disabled text.</summary>
public class TextDisabled : BaseUIElement<TextDisabled>
{
    private Ref<string>? _binding;

    public TextDisabled(string name, string text) : base(name)
    {
        Data = new TextData { Name = name, Text = text, Variant = TextType.TextDisabled };
    }

    public TextDisabled WithText(Ref<string> binding)
    {
        _binding = binding;
        ((TextData)Data).Text = binding.Value ?? "";
        binding.Changed += v => { ((TextData)Data).Text = v ?? ""; MarkChanged(); };
        return this;
    }
}

/// <summary>ImGui.TextWrapped  text that wraps at window edge.</summary>
public class TextWrapped : BaseUIElement<TextWrapped>
{
    private Ref<string>? _binding;

    public TextWrapped(string name, string text) : base(name)
    {
        Data = new TextData { Name = name, Text = text, Variant = TextType.TextWrapped };
    }

    public TextWrapped WithText(Ref<string> binding)
    {
        _binding = binding;
        ((TextData)Data).Text = binding.Value ?? "";
        binding.Changed += v => { ((TextData)Data).Text = v ?? ""; MarkChanged(); };
        return this;
    }
}

/// <summary>ImGui.LabelText  label on the left, value text on the right.</summary>
public class LabelText : BaseUIElement<LabelText>
{
    private Ref<string>? _textBinding;
    private Ref<string>? _labelBinding;

    public LabelText(string name, string label, string text) : base(name)
    {
        Data = new TextData { Name = name, Text = text, Label = label, Variant = TextType.LabelText };
    }

    public LabelText WithText(Ref<string> binding)
    {
        _textBinding = binding;
        ((TextData)Data).Text = binding.Value ?? "";
        binding.Changed += v => { ((TextData)Data).Text = v ?? ""; MarkChanged(); };
        return this;
    }

    public LabelText WithLabel(Ref<string> binding)
    {
        _labelBinding = binding;
        ((TextData)Data).Label = binding.Value ?? "";
        binding.Changed += v => { ((TextData)Data).Label = v ?? ""; MarkChanged(); };
        return this;
    }
}

/// <summary>ImGui.BulletText  bullet point with text.</summary>
public class BulletText : BaseUIElement<BulletText>
{
    private Ref<string>? _binding;

    public BulletText(string name, string text) : base(name)
    {
        Data = new TextData { Name = name, Text = text, Variant = TextType.BulletText };
    }

    public BulletText WithText(Ref<string> binding)
    {
        _binding = binding;
        ((TextData)Data).Text = binding.Value ?? "";
        binding.Changed += v => { ((TextData)Data).Text = v ?? ""; MarkChanged(); };
        return this;
    }
}

/// <summary>ImGui.SeparatorText  horizontal separator with a centered label.</summary>
public class SeparatorText : BaseUIElement<SeparatorText>
{
    private Ref<string>? _binding;

    public SeparatorText(string name, string label) : base(name)
    {
        Data = new TextData { Name = name, Text = label, Variant = TextType.SeparatorText };
    }

    public SeparatorText WithText(Ref<string> binding)
    {
        _binding = binding;
        ((TextData)Data).Text = binding.Value ?? "";
        binding.Changed += v => { ((TextData)Data).Text = v ?? ""; MarkChanged(); };
        return this;
    }
}
