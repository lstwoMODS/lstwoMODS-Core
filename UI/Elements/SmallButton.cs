using System;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class SmallButton : BaseUIElement<SmallButton>
{
    public event Action OnPressed;

    public SmallButton(string name, Action onPressed = null, bool mainThread = true) : base(name)
    {
        Data = new SmallButtonData { Name = name };
        if (onPressed != null) OnPressed += onPressed;
        RunCallbacksOnMainThread = mainThread;
    }

    public override void ApplyReceivedData(BaseUIElementData data)
    {
        base.ApplyReceivedData(data);
        if (((SmallButtonData)data).Pressed)
            InvokeCallback(() => OnPressed?.Invoke());
    }
}
