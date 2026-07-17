using Hexa.NET.ImGui;
using GLFWwindowPtr = Hexa.NET.GLFW.GLFWwindowPtr;

namespace lstwoMODS_Overlay.Backends;

public interface IRenderBackend
{
    bool IsOpenGL { get; }

    /// <summary>Called before GLFW.CreateWindow to set API-specific window hints.</summary>
    void ConfigureGlfwHints();

    /// <summary>Called after GLFW.CreateWindow to initialise the graphics API context.</summary>
    void Initialize(GLFWwindowPtr window);

    void Shutdown();

    void BeginFrame(bool isOverlay, float r, float g, float b, float a);
    void EndFrame();

    /// <summary>
    /// Initialise the ImGui renderer backend. Called after the ImGui context is current
    /// and the GLFW platform backend has already been initialised.
    /// </summary>
    bool InitImGuiRenderer();

    void ShutdownImGuiRenderer();
    void NewImGuiFrame();
    void RenderImGuiDrawData(ImDrawDataPtr drawData);

    /// <summary>Invalidate and recreate GPU font/device objects after the ImGui font atlas changes.</summary>
    void RebuildFontTexture();

    /// <summary>Called when the framebuffer is resized. No-op for backends that handle it automatically (e.g. OpenGL).</summary>
    void OnResize(int width, int height);

    nint UploadTexture(byte[] rgbaPixels, int width, int height);
    void FreeTexture(nint textureId);
}
