using System.Numerics;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class GizmoRenderer : UIRenderer
{
    private float[] _view, _proj, _model;
    private ImGuizmoOperation _op;
    private ImGuizmoMode _mode;
    private float? _snap;
    private bool _changedThisFrame;

    public GizmoRenderer(BaseUIElementData data) : base(data) { CopyFrom((GizmoData)data); }

    private void CopyFrom(GizmoData d)
    {
        _view  = (float[])d.ViewMatrix.Clone();
        _proj  = (float[])d.ProjectionMatrix.Clone();
        _model = (float[])d.ModelMatrix.Clone();
        _op    = (ImGuizmoOperation)(int)d.Operation;
        _mode  = (ImGuizmoMode)(int)d.Mode;
        _snap  = d.SnapValue;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (GizmoData)data;
        Data = d;
        Name = d.Name;
        CopyFrom(d);
    }

    public override void Render()
    {
        var io = ImGui.GetIO();
        ImGuizmo.SetRect(0, 0, io.DisplaySize.X, io.DisplaySize.Y);

        var view  = ToMatrix4x4(_view);
        var proj  = ToMatrix4x4(_proj);
        var model = ToMatrix4x4(_model);

        bool changed;
        if (_snap.HasValue)
        {
            var snap = _snap.Value;
            changed = ImGuizmo.Manipulate(ref view, ref proj, _op, _mode, ref model, ref snap);
        }
        else
        {
            changed = ImGuizmo.Manipulate(ref view, ref proj, _op, _mode, ref model);
        }

        if (changed)
        {
            _model = FromMatrix4x4(model);
            _changedThisFrame = true;
        }
    }

    public override BaseUIElementData? GetNewState()
    {
        if (!_changedThisFrame) return null;
        _changedThisFrame = false;
        var d = (GizmoData)Data;
        return new GizmoData
        {
            Id               = Data.Id,
            Name             = Data.Name,
            Enabled          = Data.Enabled,
            ViewMatrix       = _view,
            ProjectionMatrix = _proj,
            ModelMatrix      = _model,
            Operation        = d.Operation,
            Mode             = d.Mode,
            SnapValue        = _snap,
            Changed          = true
        };
    }

    private static Matrix4x4 ToMatrix4x4(float[] f) => new Matrix4x4(
        f[0], f[4], f[8],  f[12],
        f[1], f[5], f[9],  f[13],
        f[2], f[6], f[10], f[14],
        f[3], f[7], f[11], f[15]);

    private static float[] FromMatrix4x4(Matrix4x4 m) => new float[]
    {
        m.M11, m.M21, m.M31, m.M41,
        m.M12, m.M22, m.M32, m.M42,
        m.M13, m.M23, m.M33, m.M43,
        m.M14, m.M24, m.M34, m.M44
    };
}
