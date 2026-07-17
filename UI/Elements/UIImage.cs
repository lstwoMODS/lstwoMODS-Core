using System;
using UnityEngine;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

/// <summary>ImGui.Image  displays an image loaded from a file path on the overlay side.</summary>
public class UIImage : BaseUIElement<UIImage>
{
    public UIImage(string name, string filePath, float displayW = 0f, float displayH = 0f) : base(name)
    {
        Data = new ImageData
        {
            Name     = name,
            FilePath = filePath,
            DisplayW = displayW,
            DisplayH = displayH,
        };
    }

    /// <summary>Set UV rectangle for image cropping (0–1 range). Chainable.</summary>
    public UIImage WithUV(float u0, float v0, float u1, float v1)
    {
        var d = (ImageData)Data;
        d.UV0X = u0; d.UV0Y = v0;
        d.UV1X = u1; d.UV1Y = v1;
        return this;
    }

    /// <summary>Tint color multiplied over the image. Chainable.</summary>
    public UIImage WithTint(Color color)
    {
        var d = (ImageData)Data;
        d.TintR = color.r; d.TintG = color.g; d.TintB = color.b; d.TintA = color.a;
        return this;
    }
    public UIImage WithTint(float r, float g, float b, float a = 1f)
    {
        var d = (ImageData)Data;
        d.TintR = r; d.TintG = g; d.TintB = b; d.TintA = a;
        return this;
    }
}

/// <summary>ImGui.ImageButton  clickable image. Fires <see cref="OnPressed"/> on click.</summary>
public class UIImageButton : BaseUIElement<UIImageButton>
{
    public event Action OnPressed;

    public UIImageButton(string name, string filePath, float displayW = 0f, float displayH = 0f,
                         Action onPressed = null, bool mainThread = true) : base(name)
    {
        Data = new ImageData
        {
            Name     = name,
            FilePath = filePath,
            DisplayW = displayW,
            DisplayH = displayH,
            IsButton = true,
        };
        if (onPressed != null) OnPressed += onPressed;
        RunCallbacksOnMainThread = mainThread;
    }

    public UIImageButton WithUV(float u0, float v0, float u1, float v1)
    {
        var d = (ImageData)Data;
        d.UV0X = u0; d.UV0Y = v0; d.UV1X = u1; d.UV1Y = v1;
        return this;
    }
    public UIImageButton WithTint(Color color)
    {
        var d = (ImageData)Data;
        d.TintR = color.r; d.TintG = color.g; d.TintB = color.b; d.TintA = color.a;
        return this;
    }
    public UIImageButton WithBackground(Color color)
    {
        var d = (ImageData)Data;
        d.BgR = color.r; d.BgG = color.g; d.BgB = color.b; d.BgA = color.a;
        return this;
    }

    public override void ApplyReceivedData(BaseUIElementData data)
    {
        base.ApplyReceivedData(data);
        if (((ImageData)data).Pressed)
            InvokeCallback(() => OnPressed?.Invoke());
    }
}
