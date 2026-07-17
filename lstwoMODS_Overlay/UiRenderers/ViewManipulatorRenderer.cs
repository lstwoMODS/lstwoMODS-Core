using System.Numerics;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class ViewManipulatorRenderer : UIRenderer
{
    private float[] _view;
    private float _length;
    private float _posX, _posY;
    private float _sizeX, _sizeY;
    private uint _bgColor;
    private bool _changedThisFrame;

    public ViewManipulatorRenderer(BaseUIElementData data) : base(data) { CopyFrom((ViewManipulatorData)data); }

    private void CopyFrom(ViewManipulatorData d)
    {
        _view    = (float[])d.ViewMatrix.Clone();
        _length  = d.Length;
        _posX    = d.PosX;
        _posY    = d.PosY;
        _sizeX   = d.SizeX;
        _sizeY   = d.SizeY;
        _bgColor = d.BackgroundColor;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (ViewManipulatorData)data;
        Data = d;
        Name = d.Name;
        CopyFrom(d);
    }

    public override void Render()
    {
        // Auto-place in top-right of current window if pos is negative
        Vector2 pos;
        if (_posX < 0 || _posY < 0)
        {
            var windowPos  = ImGui.GetWindowPos();
            var windowSize = ImGui.GetWindowSize();
            pos = new Vector2(windowPos.X + windowSize.X - _sizeX, windowPos.Y);
        }
        else
        {
            pos = new Vector2(_posX, _posY);
        }

        var view = ToMatrix4x4(_view);
        ImGuizmo.ViewManipulate(ref view, _length, pos, new Vector2(_sizeX, _sizeY), _bgColor);

        var newView = FromMatrix4x4(view);
        if (!ArraysEqual(_view, newView))
        {
            _view = newView;
            _changedThisFrame = true;
        }
    }

    public override BaseUIElementData? GetNewState()
    {
        if (!_changedThisFrame) return null;
        _changedThisFrame = false;
        return new ViewManipulatorData
        {
            Id               = Data.Id,
            Name             = Data.Name,
            Enabled          = Data.Enabled,
            ViewMatrix       = _view,
            Length           = _length,
            PosX             = _posX,
            PosY             = _posY,
            SizeX            = _sizeX,
            SizeY            = _sizeY,
            BackgroundColor  = _bgColor,
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

    private static bool ArraysEqual(float[] a, float[] b)
    {
        if (a.Length != b.Length) return false;
        for (var i = 0; i < a.Length; i++)
            if (a[i] != b[i]) return false;
        return true;
    }
}
