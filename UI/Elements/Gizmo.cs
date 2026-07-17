using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;
public class Gizmo : BaseUIElement<Gizmo>
{
    public Action<Matrix4x4> OnChanged;

    public Matrix4x4 ModelMatrix
    {
        get => FromFloat16(((GizmoData)Data).ModelMatrix);
        set { ((GizmoData)Data).ModelMatrix = ToFloat16(value); MarkChanged(); }
    }

    /// <param name="name">Unique ID.</param>
    /// <param name="view">Camera view matrix (column-major, OpenGL convention).</param>
    /// <param name="projection">Camera projection matrix.</param>
    /// <param name="model">Object transform matrix. Updated via OnChanged when user drags the gizmo.</param>
    public Gizmo(string name, Matrix4x4 view, Matrix4x4 projection, Matrix4x4 model, ImGuizmoOperation operation = ImGuizmoOperation.Translate, ImGuizmoMode mode = ImGuizmoMode.World, Action<Matrix4x4> onChanged = null, bool mainThread = true) : base(name)
    {
        Data = new GizmoData
        {
            Name             = name,
            ViewMatrix       = ToFloat16(view),
            ProjectionMatrix = ToFloat16(projection),
            ModelMatrix      = ToFloat16(model),
            Operation        = operation,
            Mode             = mode,
        };
        OnChanged = onChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    /// <summary>Update camera matrices each frame before rendering.</summary>
    public void SetCamera(Matrix4x4 view, Matrix4x4 projection)
    {
        var d = (GizmoData)Data;
        d.ViewMatrix       = ToFloat16(view);
        d.ProjectionMatrix = ToFloat16(projection);
        MarkChanged();
    }

    public Gizmo WithOperation(ImGuizmoOperation op) { ((GizmoData)Data).Operation = op; return this; }
    public Gizmo WithMode(ImGuizmoMode mode)          { ((GizmoData)Data).Mode = mode; return this; }
    public Gizmo WithSnap(float snap)                 { ((GizmoData)Data).SnapValue = snap; return this; }

    public override void ApplyReceivedData(BaseUIElementData data)
    {
        var d = (GizmoData)data;
        var changed = d.Changed;
        base.ApplyReceivedData(data);
        if (changed)
        {
            var m = ModelMatrix;
            InvokeCallback(() => OnChanged?.Invoke(m));
        }
    }

    // ── Matrix conversion helpers ─────────────────────────────────────────────

    // Unity Matrix4x4 → column-major float[16] (matches OpenGL/ImGuizmo convention)
    private static float[] ToFloat16(Matrix4x4 m) => new float[] {
        m.m00, m.m10, m.m20, m.m30,
        m.m01, m.m11, m.m21, m.m31,
        m.m02, m.m12, m.m22, m.m32,
        m.m03, m.m13, m.m23, m.m33
    };

    // column-major float[16] → Unity Matrix4x4
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
