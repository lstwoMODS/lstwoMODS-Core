using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.Messages;

namespace lstwoMODS_Core.UI;

public static class UIManager
{
    public static Action OnConfigure;
    public static Action OnRender;
    public static int MainIpcChannelPort;
    private static string _authToken;
    public static Process Process;
    public static IpcChannel IpcChannel;
    public static Dictionary<string, OSWindow> Windows = new();

    private static readonly Dictionary<string, Action<StyleDataMessage>> _styleDataCallbacks = new();

    public static ConfigEntry<string> BackendEntry;
    private static ConfigEntry<string> _overlayExePath;
    private static Task IpcChannelMainTask;

    public static Action OnInitialized;

    /// <summary>
    /// Fires when a freshly started overlay has connected in place of one that died. The window
    /// trees are replayed by core, but anything a mod pushed across once at startup (asset
    /// catalogues, item lists, ...) died with the old process and has to be re-sent from here.
    /// Runs on the IPC connect thread, not the Unity main thread.
    /// </summary>
    public static Action OnReconnected;

    /// <summary>
    /// Inbound overlay messages, forwarded after core has handled its own types. Subscribe here
    /// rather than to <see cref="IpcChannel"/>.MessageReceived: a restart replaces the channel
    /// object, and a subscription made against the old one is dropped silently, leaving the mod
    /// unable to receive anything until the game is restarted. Runs on the IPC reader thread.
    /// </summary>
    public static event Action<IpcMessage> MessageReceived;

    // ── Overlay supervision ─────────────────────────────────────────────────
    // If the overlay process dies or its IPC channel breaks, it is restarted and every
    // window's live element tree is replayed. Restarts are capped so a persistently
    // crashing overlay can't loop: at most MaxRestartsPerWindow within RestartWindow
    // (rolling), and at most MaxRestartsPerSession overall  then we give up for the
    // session and log why.
    private const int MaxRestartsPerWindow  = 3;
    private const int MaxRestartsPerSession = 10;
    private static readonly TimeSpan RestartWindow = TimeSpan.FromSeconds(60);
    private const int RestartDelayMs = 1500;

    /// <summary>
    /// How long a freshly started overlay gets to connect back before it is treated as lost.
    /// A process that starts but never connects (its own IPC connect failed, it hung during
    /// plugin loading, ...) raises no Exited event, so without this the mod side waits in
    /// AcceptTcpClientAsync forever and the UI simply never appears.
    /// </summary>
    private const int ConnectTimeoutMs = 20000;

    private static string _exePath;
    private static bool _disposed;
    private static bool _gaveUp;
    private static bool _everConnected;
    private static int  _restartPending;               // interlocked: one restart per incident
    private static int  _generation;                   // identifies which overlay instance an event came from
    private static int  _connectedGeneration;          // last generation that actually connected back
    private static int  _totalRestarts;
    private static readonly List<DateTime> _restartTimes = new();

    public static int GetRandomFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        listener.Stop();
        return port;
    }

    private static string GenerateAuthToken()
    {
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    internal static void Initialize()
    {
        BackendEntry = Plugin.ConfigFile.Bind(
            "UI",
            "Render Backend",
            "opengl",
            "Render backend for the overlay window. Options: opengl, directx11"
        );

        _overlayExePath = Plugin.ConfigFile.Bind(
            "Internal",
            "Relative Overlay Exe File Path",
            "Overlay/lstwoMODS_Overlay.exe",
            "The relative path to the overlay exe file from the dll file."
        );

        _exePath = Path.Combine(Path.GetDirectoryName(typeof(UIManager).Assembly.Location), _overlayExePath.Value);

        if (!File.Exists(_exePath))
        {
            _exePath = Path.Combine(Path.GetDirectoryName(typeof(UIManager).Assembly.Location), "Overlay/lstwoMODS_Overlay.exe");
            _overlayExePath.Value = "Overlay/lstwoMODS_Overlay.exe";
        }

        // The fallback path is not checked anywhere else, and starting the supervisor for an exe
        // that isn't there just produces a listener nobody ever connects to  a silent, permanently
        // missing UI. Say what is wrong instead.
        if (!File.Exists(_exePath))
        {
            Plugin.LogSource.LogError(
                $"[UIManager] Overlay executable not found at '{_exePath}'  the UI cannot start. " +
                "Check that the 'Overlay' folder sits next to lstwoMODS_Core.dll and that your " +
                "antivirus has not quarantined lstwoMODS_Overlay.exe.");
            return;
        }

        StartOverlay();
    }

    /// <summary>
    /// Start (or restart) the overlay process with a fresh port, auth token and IPC channel.
    /// </summary>
    private static void StartOverlay()
    {
        var generation = Interlocked.Increment(ref _generation);

        MainIpcChannelPort = GetRandomFreePort();
        _authToken = GenerateAuthToken();

        var gameProcessId = Process.GetCurrentProcess().Id;

        var psi = new ProcessStartInfo
        {
            FileName = _exePath,
            // token is base64 (no spaces) so it stays a single argv entry.
            Arguments = $"{MainIpcChannelPort} {gameProcessId} {_authToken}",
            WorkingDirectory = Path.GetDirectoryName(_exePath),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Kept in a local as well as the static field: a failure below can start the next
        // generation while this call is still running, and the rest of this method must keep
        // operating on its own process, not on whatever replaced it.
        var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        Process = process;

        process.OutputDataReceived += (_, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                Plugin.LogSource.LogInfo($"[Overlay] {args.Data}");
            }
        };

        process.ErrorDataReceived += (_, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                Plugin.LogSource.LogError($"[Overlay] {args.Data}");
            }
        };

        process.Exited += (_, _) => OnOverlayLost(generation, "overlay process exited");

        IpcChannel = new IpcChannel(true, MainIpcChannelPort, () => OnChannelConnected(generation), _authToken);
        IpcChannel.MessageReceived += HandleMessage;
        IpcChannel.Disconnected += () => OnOverlayLost(generation, "IPC connection lost");

        IpcChannel.Log += (msg, type) => Plugin.LogSource.Log(type switch
        {
            IpcChannel.LogType.Debug => LogLevel.Debug,
            IpcChannel.LogType.Info => LogLevel.Info,
            IpcChannel.LogType.Warning => LogLevel.Warning,
            IpcChannel.LogType.Error => LogLevel.Error,
            _ => LogLevel.Info
        }, "[IPC] " + msg);

        IpcChannelMainTask = IpcChannel.Main();

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }
        catch (Exception ex)
        {
            // A failed start raises no Exited event, so nothing else would ever notice. This is
            // what a quarantined, deleted or blocked overlay exe looks like from here.
            Plugin.LogSource.LogError(
                $"[UIManager] Could not start the overlay process '{_exePath}': {ex.Message}");
            OnOverlayLost(generation, "overlay process failed to start");
            return;
        }

        WatchForConnect(generation);
    }

    /// <summary>
    /// Give this overlay generation <see cref="ConnectTimeoutMs"/> to connect back, and treat it
    /// as lost if it doesn't. Covers the case the Exited event can't: an overlay process that is
    /// still alive but will never talk to us.
    /// </summary>
    private static void WatchForConnect(int generation)
    {
        Task.Run(async () =>
        {
            await Task.Delay(ConnectTimeoutMs);

            if (_disposed || _gaveUp) return;
            if (generation != Volatile.Read(ref _generation)) return;
            if (Volatile.Read(ref _connectedGeneration) == generation) return;

            OnOverlayLost(generation, $"overlay did not connect within {ConnectTimeoutMs / 1000}s");
        });
    }

    private static void OnChannelConnected(int generation)
    {
        Volatile.Write(ref _connectedGeneration, generation);

        if (_everConnected)
        {
            // Reconnected after a restart  the mod-side element trees survived, replay
            // them so the fresh overlay shows the exact pre-crash UI (layout comes from
            // the overlay's own imgui.ini).
            Plugin.LogSource.LogInfo("[UIManager] Overlay reconnected  restoring windows.");
            foreach (var window in Windows.Values)
                window.Reinitialize();

            try { OnReconnected?.Invoke(); }
            catch (Exception ex) { Plugin.LogSource.LogError($"[UIManager] OnReconnected handler threw: {ex}"); }
            return;
        }

        _everConnected = true;

        foreach (var window in Windows.Values)
        {
            window.HotkeyManager.Sync();
        }

        OnInitialized?.Invoke();
    }

    private static Task HandleMessage(IpcMessage message)
    {
        if (message.Type == nameof(FrameRequestMessage))
        {
            var request = FrameRequestMessage.Deserialize(message);
            if (request != null && Windows.TryGetValue(request.WindowId, out var window))
                window.HandleFrameRequest(request);
        }
        else if (message.Type == nameof(KeyPressMessage))
        {
            var msg = KeyPressMessage.Deserialize(message);

            if (msg != null)
            {
                if (Windows.TryGetValue(msg.WindowId, out var hotkeyWindow))
                    hotkeyWindow.HotkeyManager.HandleOverlayKey((ImGuiKey)msg.ImGuiKey, msg.Modifiers);
            }
        }
        else if (message.Type == nameof(StyleDataMessage))
        {
            var msg = StyleDataMessage.Deserialize(message);
            if (msg != null)
            {
                Action<StyleDataMessage> callback;
                lock (_styleDataCallbacks)
                {
                    _styleDataCallbacks.TryGetValue(msg.RequestId, out callback);
                    _styleDataCallbacks.Remove(msg.RequestId);
                }
                callback?.Invoke(msg);
            }
        }

        // Per-handler catch: this runs on the IPC reader loop, and one mod throwing must not
        // take the channel down with it.
        var handlers = MessageReceived;
        if (handlers != null)
        {
            foreach (var handler in handlers.GetInvocationList())
            {
                try { ((Action<IpcMessage>)handler).Invoke(message); }
                catch (Exception ex) { Plugin.LogSource.LogError($"[UIManager] Message handler threw: {ex}"); }
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// The overlay died (process exit) or its channel broke. Both signals usually fire for
    /// the same incident, and events from an already-replaced overlay can arrive late  the
    /// generation check and the interlocked pending flag collapse them into one restart.
    /// </summary>
    private static void OnOverlayLost(int generation, string reason)
    {
        if (_disposed || _gaveUp) return;
        if (generation != Volatile.Read(ref _generation)) return;
        if (Interlocked.Exchange(ref _restartPending, 1) == 1) return;

        Task.Run(async () =>
        {
            try
            {
                lock (_restartTimes)
                {
                    _restartTimes.RemoveAll(t => DateTime.UtcNow - t > RestartWindow);
                    if (_restartTimes.Count >= MaxRestartsPerWindow || _totalRestarts >= MaxRestartsPerSession)
                    {
                        _gaveUp = true;
                        Plugin.LogSource.LogError(
                            $"[UIManager] Overlay died again ({reason}), but it was already restarted " +
                            $"{(_totalRestarts >= MaxRestartsPerSession ? $"{_totalRestarts} times this session" : $"{_restartTimes.Count} times in the last {RestartWindow.TotalSeconds:0}s")} " +
                            " giving up on the overlay UI. Restart the game to get it back.");
                        return;
                    }
                    _restartTimes.Add(DateTime.UtcNow);
                    _totalRestarts++;
                }

                Plugin.LogSource.LogWarning(
                    $"[UIManager] {reason}  restarting overlay in {RestartDelayMs}ms " +
                    $"(restart {_totalRestarts}/{MaxRestartsPerSession} this session).");

                try { IpcChannel?.Dispose(); } catch { /* already dead */ }
                try { if (Process is { HasExited: false }) Process.Kill(); } catch { /* already dead */ }

                await Task.Delay(RestartDelayMs);
                if (_disposed) return;

                StartOverlay();
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"[UIManager] Overlay restart failed: {ex}");
            }
            finally
            {
                Interlocked.Exchange(ref _restartPending, 0);
            }
        });
    }

    internal static void RequestStyleData(string windowId, int themeIndex, Action<StyleDataMessage> callback)
    {
        var requestId = Guid.NewGuid().ToString();
        lock (_styleDataCallbacks)
            _styleDataCallbacks[requestId] = callback;

        IpcChannel.SendMessage(new RequestStyleDataMessage
        {
            WindowId   = windowId,
            ThemeIndex = themeIndex,
            RequestId  = requestId,
        }.Serialize());
    }

    internal static void Dispose()
    {
        Plugin.LogSource.LogInfo("[UIManager] Disposing...");
        _disposed = true;
        IpcChannel?.Dispose();
        Plugin.LogSource.LogInfo("[UIManager] Disposed IPC Channel.");
    }
}
