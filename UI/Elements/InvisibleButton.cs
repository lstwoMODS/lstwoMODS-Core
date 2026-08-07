using System;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

/// <summary>ImGui.InvisibleButton  invisible clickable area of a given size.</summary>
public class InvisibleButton : BaseUIElement<InvisibleButton>
{
    public event Action OnPressed;

    public InvisibleButton(string name, float sizeX, float sizeY, Action onPressed = null, bool mainThread = true) : base(name)
    {
        Data = new InvisibleButtonData { Name = name, SizeX = sizeX, SizeY = sizeY };
        if (onPressed != null) OnPressed += onPressed;
        RunCallbacksOnMainThread = mainThread;
    }

    public override void ApplyReceivedData(BaseUIElementData data)
    {
        base.ApplyReceivedData(data);
        if (((InvisibleButtonData)data).Pressed)
            InvokeCallback(() => OnPressed?.Invoke());
    }
}
