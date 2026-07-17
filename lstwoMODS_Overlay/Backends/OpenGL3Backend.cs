using Hexa.NET.GLFW;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Hexa.NET.OpenGL;
using HexaGen.Runtime;
using GLFWwindowPtr = Hexa.NET.GLFW.GLFWwindowPtr;

namespace lstwoMODS_Overlay.Backends;

public class OpenGL3Backend : IRenderBackend
{
    private GL _gl = null!;
    private GLFWwindowPtr _window;

    private const string GlslVersion = "#version 150";

    public bool IsOpenGL => true;

    public void ConfigureGlfwHints()
    {
        GLFW.WindowHint(GLFW.GLFW_CONTEXT_VERSION_MAJOR, 3);
        GLFW.WindowHint(GLFW.GLFW_CONTEXT_VERSION_MINOR, 2);
        GLFW.WindowHint(GLFW.GLFW_OPENGL_PROFILE, GLFW.GLFW_OPENGL_CORE_PROFILE);
    }

    public void Initialize(GLFWwindowPtr window)
    {
        _window = window;
        _gl = new GL(new GlfwBindingsContext(window));
        _gl.Enable(GLEnableCap.Blend);
        _gl.BlendFunc(GLBlendingFactor.SrcAlpha, GLBlendingFactor.OneMinusSrcAlpha);
    }

    public void Shutdown()
    {
        _gl.Dispose();
    }

    public void BeginFrame(bool isOverlay, float r, float g, float b, float a)
    {
        GLFW.MakeContextCurrent(_window);
        if (isOverlay)
            _gl.ClearColor(0, 0, 0, 0);
        else
            _gl.ClearColor(r, g, b, a);
        _gl.Clear(GLClearBufferMask.ColorBufferBit);
    }

    public void EndFrame()
    {
        GLFW.MakeContextCurrent(_window);
        GLFW.SwapBuffers(_window);
    }

    public bool InitImGuiRenderer()
    {
        ImGuiImplOpenGL3.SetCurrentContext(ImGui.GetCurrentContext());
        return ImGuiImplOpenGL3.Init(GlslVersion);
    }

    public void ShutdownImGuiRenderer()
    {
        ImGuiImplOpenGL3.Shutdown();
        ImGuiImplOpenGL3.SetCurrentContext(null);
    }

    public void NewImGuiFrame()
    {
        ImGuiImplOpenGL3.NewFrame();
    }

    public void RenderImGuiDrawData(ImDrawDataPtr drawData)
    {
        ImGuiImplOpenGL3.RenderDrawData(drawData);
    }

    public void RebuildFontTexture()
    {
        ImGuiImplOpenGL3.DestroyDeviceObjects();
        ImGuiImplOpenGL3.CreateDeviceObjects();
    }

    public unsafe nint UploadTexture(byte[] rgbaPixels, int width, int height)
    {
        const int GL_TEXTURE_2D         = 0x0DE1;
        const int GL_TEXTURE_MIN_FILTER = 0x2801;
        const int GL_TEXTURE_MAG_FILTER = 0x2800;
        const int GL_TEXTURE_WRAP_S     = 0x2802;
        const int GL_TEXTURE_WRAP_T     = 0x2803;
        const int GL_LINEAR             = 0x2601;
        const int GL_CLAMP_TO_EDGE      = 0x812F;
        const int GL_RGBA8              = 0x8058;
        const int GL_RGBA               = 0x1908;
        const int GL_UNSIGNED_BYTE      = 0x1401;

        var texId = _gl.GenTexture();
        _gl.BindTexture((GLTextureTarget)GL_TEXTURE_2D, texId);
        _gl.TexParameteri((GLTextureTarget)GL_TEXTURE_2D, (GLTextureParameterName)GL_TEXTURE_MIN_FILTER, GL_LINEAR);
        _gl.TexParameteri((GLTextureTarget)GL_TEXTURE_2D, (GLTextureParameterName)GL_TEXTURE_MAG_FILTER, GL_LINEAR);
        _gl.TexParameteri((GLTextureTarget)GL_TEXTURE_2D, (GLTextureParameterName)GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
        _gl.TexParameteri((GLTextureTarget)GL_TEXTURE_2D, (GLTextureParameterName)GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
        fixed (byte* ptr = rgbaPixels)
            _gl.TexImage2D((GLTextureTarget)GL_TEXTURE_2D, 0, (GLInternalFormat)GL_RGBA8, width, height, 0,
                (GLPixelFormat)GL_RGBA, (GLPixelType)GL_UNSIGNED_BYTE, ptr);
        _gl.BindTexture((GLTextureTarget)GL_TEXTURE_2D, 0);

        return (nint)texId;
    }

    public void FreeTexture(nint textureId)
    {
        var id = (uint)textureId;
        _gl.DeleteTexture(id);
    }

    public void OnResize(int width, int height) { }
}
