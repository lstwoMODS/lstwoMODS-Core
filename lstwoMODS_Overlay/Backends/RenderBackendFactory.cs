namespace lstwoMODS_Overlay.Backends;

public static class RenderBackendFactory
{
    public static IRenderBackend Create(string backendName) =>
        backendName.Trim().ToLowerInvariant() switch
        {
            "directx11" or "dx11" or "d3d11" => new DirectX11Backend(),
            _ => new OpenGL3Backend()
        };
}
