using System.Numerics;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class ImageRenderer : UIRenderer
{
    private string _filePath;
    private float  _displayW, _displayH;
    private float  _uv0X, _uv0Y, _uv1X, _uv1Y;
    private float  _tintR, _tintG, _tintB, _tintA;
    private float  _bgR, _bgG, _bgB, _bgA;
    private bool   _isButton;
    private bool   _pressedThisFrame;

    // Lazily resolved on first Render() call (render thread only)
    private nint _texId   = -1;
    private int  _naturalW;
    private int  _naturalH;

    public ImageRenderer(BaseUIElementData data) : base(data) { CopyFrom((ImageData)data); }

    private void CopyFrom(ImageData d)
    {
        _filePath = d.FilePath;
        _displayW = d.DisplayW; _displayH = d.DisplayH;
        _uv0X = d.UV0X; _uv0Y = d.UV0Y; _uv1X = d.UV1X; _uv1Y = d.UV1Y;
        _tintR = d.TintR; _tintG = d.TintG; _tintB = d.TintB; _tintA = d.TintA;
        _bgR   = d.BgR;   _bgG   = d.BgG;   _bgB   = d.BgB;   _bgA   = d.BgA;
        _isButton = d.IsButton;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (ImageData)data; Data = d; Name = d.Name;
        // If the path changed, reset texture so it's reloaded
        if (_filePath != d.FilePath) _texId = -1;
        CopyFrom(d);
    }

    public override unsafe void Render()
    {
        // Lazily load texture on first call (render thread has OpenGL context)
        if (_texId == -1)
        {
            _texId = Window.LoadTexture(_filePath, out _naturalW, out _naturalH);
            if (_texId == -1) return;
        }

        // ImGui.ImTextureRef returns ImTextureRefPtr. ImTextureRefPtr implicitly converts
        // to ImTextureRef*  dereference to get the ImTextureRef value for Image/ImageButton.
        var texRefPtrW = ImGui.ImTextureRef(new ImTextureID((ulong)_texId));
        ImTextureRef* rawRef = texRefPtrW; // implicit op_Implicit(ImTextureRefPtr) -> ImTextureRef*
        if (rawRef == null) return;

        var size = new Vector2(
            _displayW > 0 ? _displayW : _naturalW,
            _displayH > 0 ? _displayH : _naturalH);
        var uv0  = new Vector2(_uv0X, _uv0Y);
        var uv1  = new Vector2(_uv1X, _uv1Y);
        var tint = new Vector4(_tintR, _tintG, _tintB, _tintA);

        if (_isButton)
        {
            var bg = new Vector4(_bgR, _bgG, _bgB, _bgA);
            _pressedThisFrame |= ImGui.ImageButton(Data.Name, *rawRef, size, uv0, uv1, bg, tint);
        }
        else
        {
            ImGui.Image(*rawRef, size, uv0, uv1);
        }
    }

    public override BaseUIElementData? GetNewState()
    {
        if (!_isButton || !_pressedThisFrame) return null;
        _pressedThisFrame = false;
        var d = (ImageData)Data;
        return new ImageData
        {
            Id = Data.Id, Name = Data.Name, Enabled = Data.Enabled,
            FilePath = _filePath, DisplayW = _displayW, DisplayH = _displayH,
            UV0X = _uv0X, UV0Y = _uv0Y, UV1X = _uv1X, UV1Y = _uv1Y,
            TintR = _tintR, TintG = _tintG, TintB = _tintB, TintA = _tintA,
            BgR = _bgR, BgG = _bgG, BgB = _bgB, BgA = _bgA,
            IsButton = true, Pressed = true,
        };
    }
}
