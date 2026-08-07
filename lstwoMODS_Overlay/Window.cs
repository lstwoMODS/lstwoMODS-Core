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

    public void Close() => IsRunning = false;

    public void Join(int timeoutMs = Timeout.Infinite) => _thread?.Join(timeoutMs);
    
    /// <summary>
    /// Frames failing in a row before the window gives up and shuts down. Per-frame recovery
    /// lives in RenderFrame; anything reaching the loop's catch is low-level (backend/context),
    /// so after ~a second of solid failures a clean exit  which lets the mod side restart the
    /// overlay  beats looping broken forever.
    /// </summary>
    private const int MaxConsecutiveFrameFailures = 60;

    public void Run()
    {
        try
        {
            if (!CreateWindow())
                return;

            if (!CreateGraphicsContext())
                return;

            MainScale = GetMainScale();

            if (!InitializeImGui())
                return;

            OnPreFirstFrame();

            var consecutiveFrameFailures = 0;
            IsRunning = true;
            while (IsRunning && !ShouldClose() && !TestProgram.shouldClose)
            {
                try
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
                    consecutiveFrameFailures = 0;
                }
                catch (Exception ex)
                {
                    consecutiveFrameFailures++;
                    CrashGuard.Report($"render loop ({GetType().Name})", ex);
                    if (consecutiveFrameFailures >= MaxConsecutiveFrameFailures)
                    {
                        Logger.LogError(
                            $"{consecutiveFrameFailures} consecutive frame failures  closing window \"{GetType().Name}\".");
                        break;
                    }
                }
            }

            ShutdownImGui();
            DestroyGraphicsContext();
            DestroyWindow();
        }
        catch (Exception ex)
        {
            // Without this the CLR would print a raw crash dump and kill the whole process
            // from this thread with no cleanup.
            Logger.LogError($"Fatal error in window thread: {ex}");
        }
        finally
        {
            IsRunning = false;
            // A dead render window makes the overlay useless  exit the process so the mod
            // side notices and can restart it. (No-op on normal shutdown, which is already
            // driven by ShouldClose.)
            Program.ShouldClose = true;
        }
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