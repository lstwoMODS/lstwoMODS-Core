using System.Runtime.InteropServices;
using Hexa.NET.GLFW;
using Hexa.NET.ImGui;
using GLFWwindowPtr  = Hexa.NET.GLFW.GLFWwindowPtr;
using ImGuiD3D11     = Hexa.NET.ImGui.Backends.D3D11.ImGuiImplD3D11;
using BackendDevice  = Hexa.NET.ImGui.Backends.D3D11.ID3D11Device;
using BackendContext = Hexa.NET.ImGui.Backends.D3D11.ID3D11DeviceContext;

namespace lstwoMODS_Overlay.Backends;

public unsafe class DirectX11Backend : IRenderBackend
{
    private nint _device;    // ID3D11Device*
    private nint _devCtx;   // ID3D11DeviceContext*
    private nint _swapChain; // IDXGISwapChain*
    private nint _rtv;       // ID3D11RenderTargetView*

    public bool IsOpenGL => false;

    // ── COM vtable dispatch ───────────────────────────────────────────────────

    private static nint Vtable(nint obj, int idx)
        => ((nint*)*(nint*)obj)[idx];

    private static void ComRelease(nint obj)
    {
        if (obj == 0) return;
        ((delegate* unmanaged[Stdcall]<nint, uint>)Vtable(obj, 2))(obj);
    }

    // ID3D11DeviceContext::OMSetRenderTargets  (vtable slot 33)
    private void OMSetRenderTargets(nint rtv)
    {
        var fn = (delegate* unmanaged[Stdcall]<nint, uint, nint*, nint, void>)Vtable(_devCtx, 33);
        fn(_devCtx, 1u, &rtv, 0);
    }

    // ID3D11DeviceContext::ClearRenderTargetView  (vtable slot 50)
    private void ClearRTV(float* color)
    {
        var fn = (delegate* unmanaged[Stdcall]<nint, nint, float*, void>)Vtable(_devCtx, 50);
        fn(_devCtx, _rtv, color);
    }

    // ID3D11Device::CreateRenderTargetView  (vtable slot 9)
    private int CreateRTV(nint resource, out nint rtv)
    {
        fixed (nint* pRtv = &rtv)
        {
            var fn = (delegate* unmanaged[Stdcall]<nint, nint, nint, nint*, int>)Vtable(_device, 9);
            return fn(_device, resource, 0, pRtv);
        }
    }

    // ID3D11Device::CreateTexture2D  (vtable slot 5)
    private int CreateTexture2D(D3D11_TEXTURE2D_DESC* desc, D3D11_SUBRESOURCE_DATA* data, out nint tex)
    {
        fixed (nint* pTex = &tex)
        {
            var fn = (delegate* unmanaged[Stdcall]<nint, D3D11_TEXTURE2D_DESC*, D3D11_SUBRESOURCE_DATA*, nint*, int>)Vtable(_device, 5);
            return fn(_device, desc, data, pTex);
        }
    }

    // ID3D11Device::CreateShaderResourceView  (vtable slot 7)
    private int CreateSRV(nint resource, D3D11_SRV_DESC* desc, out nint srv)
    {
        fixed (nint* pSrv = &srv)
        {
            var fn = (delegate* unmanaged[Stdcall]<nint, nint, D3D11_SRV_DESC*, nint*, int>)Vtable(_device, 7);
            return fn(_device, resource, desc, pSrv);
        }
    }

    // IDXGISwapChain::Present   (vtable slot 8)
    private void Present(uint syncInterval)
    {
        var fn = (delegate* unmanaged[Stdcall]<nint, uint, uint, int>)Vtable(_swapChain, 8);
        fn(_swapChain, syncInterval, 0u);
    }

    // IDXGISwapChain::GetBuffer  (vtable slot 9)
    private int GetBuffer(Guid* riid, out nint surface)
    {
        fixed (nint* pSurface = &surface)
        {
            var fn = (delegate* unmanaged[Stdcall]<nint, uint, Guid*, nint*, int>)Vtable(_swapChain, 9);
            return fn(_swapChain, 0u, riid, pSurface);
        }
    }

    // ── D3D11 / DXGI structs ─────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    private struct DXGI_RATIONAL { public uint Numerator, Denominator; }

    [StructLayout(LayoutKind.Sequential)]
    private struct DXGI_MODE_DESC
    {
        public uint Width, Height;
        public DXGI_RATIONAL RefreshRate;
        public uint Format, ScanlineOrdering, Scaling;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DXGI_SAMPLE_DESC { public uint Count, Quality; }

    // matches DXGI_SWAP_CHAIN_DESC (natural alignment: 4-byte padding before OutputWindow on x64)
    [StructLayout(LayoutKind.Sequential)]
    private struct DXGI_SWAP_CHAIN_DESC
    {
        public DXGI_MODE_DESC    BufferDesc;
        public DXGI_SAMPLE_DESC  SampleDesc;
        public uint BufferUsage, BufferCount;
        public nint OutputWindow;
        public int  Windowed;
        public uint SwapEffect, Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct D3D11_TEXTURE2D_DESC
    {
        public uint Width, Height, MipLevels, ArraySize, Format;
        public uint SampleCount, SampleQuality;
        public uint Usage, BindFlags, CPUAccessFlags, MiscFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct D3D11_SUBRESOURCE_DATA
    {
        public nint pSysMem;
        public uint SysMemPitch, SysMemSlicePitch;
    }

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    private struct D3D11_SRV_DESC
    {
        [FieldOffset(0)]  public uint Format;
        [FieldOffset(4)]  public uint ViewDimension;
        [FieldOffset(8)]  public uint MostDetailedMip;
        [FieldOffset(12)] public uint MipLevels;
    }

    // ── P/Invoke ──────────────────────────────────────────────────────────────

    [DllImport("d3d11.dll", CallingConvention = CallingConvention.Winapi)]
    private static extern int D3D11CreateDeviceAndSwapChain(
        nint pAdapter, int driverType, nint software, uint flags,
        nint pFeatureLevels, uint featureLevels, uint sdkVersion,
        DXGI_SWAP_CHAIN_DESC* pSwapChainDesc,
        out nint ppSwapChain, out nint ppDevice,
        int* pFeatureLevel, out nint ppImmediateContext);

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public void ConfigureGlfwHints()
    {
        GLFW.WindowHint(GLFW.GLFW_CLIENT_API, GLFW.GLFW_NO_API);
    }

    public void Initialize(GLFWwindowPtr window)
    {
        var hwnd = GLFW.GetWin32Window(window);

        var sc = new DXGI_SWAP_CHAIN_DESC
        {
            BufferDesc = new DXGI_MODE_DESC
            {
                Width = 0, Height = 0,
                RefreshRate = new DXGI_RATIONAL { Numerator = 60, Denominator = 1 },
                Format = 28, // DXGI_FORMAT_R8G8B8A8_UNORM
            },
            SampleDesc   = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
            BufferUsage  = 0x20u,  // DXGI_USAGE_RENDER_TARGET_OUTPUT
            BufferCount  = 2,
            OutputWindow = hwnd,
            Windowed     = 1,
            SwapEffect   = 0,     // DXGI_SWAP_EFFECT_DISCARD
        };

        var hr = D3D11CreateDeviceAndSwapChain(
            0, 1 /* D3D_DRIVER_TYPE_HARDWARE */, 0, 0, 0, 0,
            7 /* D3D11_SDK_VERSION */,
            &sc, out _swapChain, out _device, null, out _devCtx);

        if (hr < 0 || _device == 0 || _devCtx == 0 || _swapChain == 0)
            throw new Exception($"D3D11CreateDeviceAndSwapChain failed: 0x{(uint)hr:X8}");

        CreateRenderTarget();

        if (_rtv == 0)
            throw new Exception("D3D11 render target view creation failed");
    }

    private static readonly Guid IID_Texture2D = new("6f15aaf2-d208-4e89-9ab4-489535d34f9c");

    private void CreateRenderTarget()
    {
        var iid = IID_Texture2D;
        if (GetBuffer(&iid, out var backBuf) < 0 || backBuf == 0) return;
        CreateRTV(backBuf, out _rtv);
        ComRelease(backBuf);
    }

    private void CleanupRenderTarget()
    {
        ComRelease(_rtv);
        _rtv = 0;
    }

    public void Shutdown()
    {
        CleanupRenderTarget();
        ComRelease(_swapChain); _swapChain = 0;
        ComRelease(_devCtx);    _devCtx    = 0;
        ComRelease(_device);    _device    = 0;
    }

    // IDXGISwapChain::ResizeBuffers  (vtable slot 13)
    private int ResizeBuffers(uint w, uint h)
    {
        var fn = (delegate* unmanaged[Stdcall]<nint, uint, uint, uint, uint, uint, int>)Vtable(_swapChain, 13);
        return fn(_swapChain, 0u, w, h, 0u /* keep format */, 0u);
    }

    public void OnResize(int width, int height)
    {
        if (_swapChain == 0 || width <= 0 || height <= 0) return;
        nint nullRtv = 0;
        var unbind = (delegate* unmanaged[Stdcall]<nint, uint, nint*, nint, void>)Vtable(_devCtx, 33);
        unbind(_devCtx, 1u, &nullRtv, 0);
        CleanupRenderTarget();
        ResizeBuffers((uint)width, (uint)height);
        CreateRenderTarget();
    }

    public void BeginFrame(bool isOverlay, float r, float g, float b, float a)
    {
        OMSetRenderTargets(_rtv);
        var color = stackalloc float[4];
        if (isOverlay) { color[0] = color[1] = color[2] = color[3] = 0f; }
        else           { color[0] = r; color[1] = g; color[2] = b; color[3] = a; }
        ClearRTV(color);
    }

    public void EndFrame() => Present(1u);

    public bool InitImGuiRenderer()
    {
        ImGuiD3D11.SetCurrentContext(ImGui.GetCurrentContext());
        ref var device = ref *(BackendDevice*)_device;
        ref var ctx    = ref *(BackendContext*)_devCtx;
        return ImGuiD3D11.Init(ref device, ref ctx);
    }

    public void ShutdownImGuiRenderer()
    {
        ImGuiD3D11.Shutdown();
        ImGuiD3D11.SetCurrentContext(default);
    }

    public void NewImGuiFrame() => ImGuiD3D11.NewFrame();

    public void RenderImGuiDrawData(ImDrawDataPtr drawData) => ImGuiD3D11.RenderDrawData(drawData);

    public void RebuildFontTexture()
    {
        ImGuiD3D11.InvalidateDeviceObjects();
        ImGuiD3D11.CreateDeviceObjects();
    }

    public nint UploadTexture(byte[] rgbaPixels, int width, int height)
    {
        fixed (byte* pPixels = rgbaPixels)
        {
            var texDesc = new D3D11_TEXTURE2D_DESC
            {
                Width = (uint)width, Height = (uint)height,
                MipLevels = 1, ArraySize = 1,
                Format = 28,    // DXGI_FORMAT_R8G8B8A8_UNORM
                SampleCount = 1, SampleQuality = 0,
                Usage = 0,      // D3D11_USAGE_DEFAULT
                BindFlags = 8,  // D3D11_BIND_SHADER_RESOURCE
            };
            var initData = new D3D11_SUBRESOURCE_DATA
            {
                pSysMem = (nint)pPixels,
                SysMemPitch = (uint)(width * 4),
            };

            if (CreateTexture2D(&texDesc, &initData, out var tex) < 0 || tex == 0)
                return 0;

            var srvDesc = new D3D11_SRV_DESC
            {
                Format = 28,       // DXGI_FORMAT_R8G8B8A8_UNORM
                ViewDimension = 4, // D3D11_SRV_DIMENSION_TEXTURE2D
                MostDetailedMip = 0,
                MipLevels = 1,
            };

            CreateSRV(tex, &srvDesc, out var srv);
            ComRelease(tex);
            return srv;
        }
    }

    public void FreeTexture(nint textureId) => ComRelease(textureId);
}
