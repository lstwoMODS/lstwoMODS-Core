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

    private static string _exePath;
    private static bool _disposed;
    private static bool _gaveUp;
    private static bool _everConnected;
    private static int  _restartPending;               // interlocked: one restart per incident
    private static int  _generation;                   // identifies which overlay instance an event came from
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

        Process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        Process.OutputDataReceived += (_, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                Plugin.LogSource.LogInfo($"[Overlay] {args.Data}");
            }
        };

        Process.ErrorDataReceived += (_, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                Plugin.LogSource.LogError($"[Overlay] {args.Data}");
            }
        };

        Process.Exited += (_, _) => OnOverlayLost(generation, "overlay process exited");

        IpcChannel = new IpcChannel(true, MainIpcChannelPort, OnChannelConnected, _authToken);
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

        Process.Start();
        Process.BeginOutputReadLine();
        Process.BeginErrorReadLine();
    }

    private static void OnChannelConnected()
    {
        if (_everConnected)
        {
            // Reconnected after a restart  the mod-side element trees survived, replay
            // them so the fresh overlay shows the exact pre-crash UI (layout comes from
            // the overlay's own imgui.ini).
            Plugin.LogSource.LogInfo("[UIManager] Overlay reconnected  restoring windows.");
            foreach (var window in Windows.Values)
                window.Reinitialize();
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
