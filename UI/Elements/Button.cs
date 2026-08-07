using System;
using System.Collections.Generic;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class Button : BaseUIElement<Button>
{
    public event Action OnPressed;

    /// <param name="mainThread">
    /// When true (default), OnPressed fires on Unity's main thread via Plugin.Update().
    /// Set false to fire immediately on the IPC thread, survives game freezes but unsafe for Unity APIs.
    /// </param>
    public Button(string name, Action onPressed = null, bool mainThread = true) : base(name)
    {
        Data = new ButtonData { Name = name };
        if (onPressed != null)
            OnPressed += onPressed;
        RunCallbacksOnMainThread = mainThread;
    }

    /// <summary>Make the button width match <c>ImGui.CalcItemWidth()</c>, aligns it with input widgets in the same layout.</summary>
    public Button WithContentWidth(bool value = true) { ((ButtonData)Data).UseContentWidth = value; return this; }

    /// <summary>Bind the content-width flag to a <see cref="Ref{T}"/>.</summary>
    public Button WithContentWidth(Ref<bool> binding)
    {
        ((ButtonData)Data).UseContentWidth = binding.Value;
        binding.Changed += v => { ((ButtonData)Data).UseContentWidth = v; MarkChanged(); };
        return this;
    }

    public override IEnumerable<BaseUIElement> GetChildren() => [];

    public override void ApplyReceivedData(BaseUIElementData data)
    {
        base.ApplyReceivedData(data);
        if (((ButtonData)data).Pressed)
            InvokeCallback(() => OnPressed?.Invoke());
    }
}
