using System;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class ArrowButton : BaseUIElement<ArrowButton>
{
    public event Action OnPressed;

    public ArrowButton(string name, ImGuiDir direction = ImGuiDir.Right, Action onPressed = null, bool mainThread = true) : base(name)
    {
        Data = new ArrowButtonData { Name = name, Dir = direction };
        if (onPressed != null) OnPressed += onPressed;
        RunCallbacksOnMainThread = mainThread;
    }

    public override void ApplyReceivedData(BaseUIElementData data)
    {
        base.ApplyReceivedData(data);
        if (((ArrowButtonData)data).Pressed)
            InvokeCallback(() => OnPressed?.Invoke());
    }
}
