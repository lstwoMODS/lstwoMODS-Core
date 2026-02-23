namespace lstwoMODS_Overlay;

public abstract class Window
{
    protected Thread _thread;

    protected float MainScale;

    public bool IsRunning;

    public void StartThread()
    {
        _thread = new Thread(Run);
        _thread.Start();
    }
    
    public void Run()
    {
        if (!CreateWindow())
            return;

        if (!CreateGraphicsContext())
            return;

        MainScale = GetMainScale();

        if (!InitializeImGui())
            return;

        OnPreFirstFrame();

        IsRunning = true;
        while (IsRunning && !ShouldClose() && !TestProgram.shouldClose)
        {
            PollEvents();

            if (IsMinimized())
            {
                OnIfMinimized();
                continue;
            }

            BeginFrame();
            RenderFrame();
            EndFrame();
        }

        ShutdownImGui();
        DestroyGraphicsContext();
        DestroyWindow();
    }

    // ---- Window lifecycle ----
    protected abstract bool CreateWindow();
    protected abstract void DestroyWindow();
    protected abstract bool ShouldClose();
    protected abstract void PollEvents();
    protected abstract bool IsMinimized();

    // ---- Graphics ----
    protected abstract bool CreateGraphicsContext();
    protected abstract void DestroyGraphicsContext();
    protected abstract void BeginFrame();
    protected abstract void EndFrame();

    // ---- ImGui ----
    protected abstract bool InitializeImGui();
    protected abstract void ShutdownImGui();

    // ---- Hooks ----
    protected abstract void OnPreFirstFrame();
    protected abstract void OnIfMinimized();
    protected abstract void RenderFrame();
    protected abstract float GetMainScale();
}