using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;
public class ViewManipulator : BaseUIElement<ViewManipulator>
{
    public Action<Matrix4x4> OnChanged;

    public ViewManipulator(string name, Matrix4x4 view, float length = 1f, float posX = -1f, float posY = -1f, float sizeX = 128f, float sizeY = 128f, Action<Matrix4x4> onChanged = null, bool mainThread = true) : base(name)
    {
        Data = new ViewManipulatorData { Name = name, ViewMatrix = ToFloat16(view), Length = length, PosX = posX, PosY = posY, SizeX = sizeX, SizeY = sizeY };
        OnChanged = onChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public void SetView(Matrix4x4 view) { ((ViewManipulatorData)Data).ViewMatrix = ToFloat16(view); MarkChanged(); }

    public override void ApplyReceivedData(BaseUIElementData data)
    {
        var d = (ViewManipulatorData)data;
        var changed = d.Changed;
        base.ApplyReceivedData(data);
        if (changed)
        {
            var m = FromFloat16(((ViewManipulatorData)Data).ViewMatrix);
            InvokeCallback(() => OnChanged?.Invoke(m));
        }
    }

    private static float[] ToFloat16(Matrix4x4 m) => new float[] {
        m.m00, m.m10, m.m20, m.m30, m.m01, m.m11, m.m21, m.m31,
        m.m02, m.m12, m.m22, m.m32, m.m03, m.m13, m.m23, m.m33
    };

    private static Matrix4x4 FromFloat16(float[] f)
    {
        var m = new Matrix4x4();
        m.m00 = f[0];  m.m10 = f[1];  m.m20 = f[2];  m.m30 = f[3];
        m.m01 = f[4];  m.m11 = f[5];  m.m21 = f[6];  m.m31 = f[7];
        m.m02 = f[8];  m.m12 = f[9];  m.m22 = f[10]; m.m32 = f[11];
        m.m03 = f[12]; m.m13 = f[13]; m.m23 = f[14]; m.m33 = f[15];
        return m;
    }
}
