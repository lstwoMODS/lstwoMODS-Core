
using System.Diagnostics;
using System.IO;
using System.Net;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.Messages;
using lstwoMODS.ImGui.Shared.UI;
using lstwoMODS_Overlay.Backends;

namespace lstwoMODS_Overlay
{
    public class Program
    {
        public static bool ShouldClose = false;
        public static int MainIpcChannelPort;
        public static IpcChannel IpcChannel;
        public static PluginManager PluginManager;
        public static OverlayConfig OverlayConfig = new();

        private static bool _isChannelInitialized = false;
        private static Dictionary<string, RemoteImGuiWindow> _remoteWindows = new();

        static void Main(string[] args)
        {
            // Last-resort logging: anything that still escapes a thread gets one readable
            // line (relayed to the game log by the mod side) instead of the CLR's raw
            // multi-line crash dump on stderr.
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                Logger.LogError($"FATAL unhandled exception: {e.ExceptionObject}");

            Logger.Log("lstwoMODS Overlay starting...");

            try
            {
                OverlayConfig = OverlayConfig.Load();
                Logger.Log($"Render backend: {OverlayConfig.Backend}");

                MainIpcChannelPort = int.Parse(args[0]);
                var gameProcessId  = args.Length > 1 ? int.Parse(args[1]) : -1;
                var authToken      = args.Length > 2 ? args[2] : null;

                var ctx = new OverlayContext();
                PluginManager = new PluginManager(ctx);
                PluginManager.LoadFromDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins"));

                IpcChannel = new IpcChannel(false, MainIpcChannelPort, () => _isChannelInitialized = true, authToken);
                IpcChannel.MessageReceived += MessageReceived;
                IpcChannel.Disconnected += () => ShouldClose = true;
                IpcChannel.Log += LogHandler;
                _ = IpcChannel.Main();

                var gameProcess = gameProcessId > 0 ? Process.GetProcessById(gameProcessId) : null;

                while (!ShouldClose)
                {
                    Thread.Sleep(100);

                    try
                    {
                        if (gameProcess != null && gameProcess.HasExited)
                            ShouldClose = true;
                    }
                    catch
                    {
                        ShouldClose = true;
                    }
                }

                RemoteImGuiWindow[] windows;
                lock (_remoteWindows)
                {
                    windows = _remoteWindows.Values.ToArray();
                    foreach (var w in windows)
                        w.Close();
                }
                foreach (var w in windows)
                    w.Join(2000);

                PluginManager.Shutdown();
                IpcChannel.Dispose();

                Logger.Log("Overlay shut down successfully");
            }
            catch (Exception ex)
            {
                Logger.Log($"Fatal error in overlay: {ex.Message}");
                Logger.Log($"Stack trace: {ex.StackTrace}");
            }
        }

        private static void LogHandler(string message, IpcChannel.LogType logType)
        {
            message = "[IPC] " + message;
            switch (logType)
            {
                case IpcChannel.LogType.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(message);
                    Console.ResetColor();
                    break;
                case IpcChannel.LogType.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(message);
                    Console.ResetColor();
                    break;
                case IpcChannel.LogType.Info:
                    Console.WriteLine(message);
                    break;
                case IpcChannel.LogType.Debug:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine(message);
                    Console.ResetColor();
                    break;
            }
        }

        private static async Task MessageReceived(IpcMessage message)
        {
            try
            {
                switch (message.Type)
                {
                    case nameof(WindowInitMessage):
                    {
                        var deserialized = WindowInitMessage.Deserialize(message);

                        if (deserialized == null)
                        {
                            break;
                        }

                        var window = new RemoteImGuiWindow("", () => { }, () => { }, deserialized.Title,
                            deserialized.Width, deserialized.Height, type: deserialized.WindowType,
                            iconPath: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.png"),
                            backend: RenderBackendFactory.Create(deserialized.Backend ?? OverlayConfig.Backend));
                        window.WindowId = deserialized.WindowId;
                        window.InitializeElements(deserialized.Elements);
                        window.Config          = deserialized.Config ?? new lstwoMODS.ImGui.Shared.ImGuiConfig();
                        window.FontDescriptors = deserialized.Fonts?.ToList() ?? new List<FontDescriptor>();
                        window.StartThread();
                        window.TrackWindow(new IntPtr(deserialized.FollowWindowHandle));
                        PluginManager?.NotifyWindowCreated(window);

                        lock (_remoteWindows)
                        {
                            _remoteWindows[deserialized.WindowId] = window;
                        }

                        break;
                    }
                    case nameof(FrameStateMessage):
                    {
                        var deserialized = FrameStateMessage.Deserialize(message);
                        if (deserialized == null) break;

                        lock (_remoteWindows)
                        {
                            if (_remoteWindows.TryGetValue(deserialized.WindowId, out var window))
                                window.ApplyFrameState(deserialized);
                        }

                        break;
                    }
                    case nameof(SetImGuiConfigMessage):
                    {
                        var deserialized = SetImGuiConfigMessage.Deserialize(message);
                        if (deserialized == null) break;

                        lock (_remoteWindows)
                        {
                            if (_remoteWindows.TryGetValue(deserialized.WindowId, out var window))
                                window.ApplyConfig(deserialized.Config);
                        }

                        break;
                    }
                    case nameof(LoadIniSettingsMessage):
                    {
                        var deserialized = LoadIniSettingsMessage.Deserialize(message);
                        if (deserialized == null) break;
                        lock (_remoteWindows)
                        {
                            if (_remoteWindows.TryGetValue(deserialized.WindowId, out var window))
                                window.QueueLoadIniSettings(deserialized.IniContent);
                        }
                        break;
                    }
                    case nameof(RegisterFontMessage):
                    {
                        var deserialized = RegisterFontMessage.Deserialize(message);
                        if (deserialized == null) break;
                        lock (_remoteWindows)
                        {
                            if (_remoteWindows.TryGetValue(deserialized.WindowId, out var window))
                                window.QueueFontRegistration(new FontDescriptor
                                {
                                    Name         = deserialized.Name,
                                    FilePath     = deserialized.FilePath,
                                    Size         = deserialized.Size,
                                    Merge        = deserialized.Merge,
                                    GlyphOffsetY = deserialized.GlyphOffsetY
                                });
                        }
                        break;
                    }
                    case nameof(PreloadImageMessage):
                    {
                        var deserialized = PreloadImageMessage.Deserialize(message);
                        if (deserialized == null) break;
                        lock (_remoteWindows)
                        {
                            if (_remoteWindows.TryGetValue(deserialized.WindowId, out var window))
                                window.QueueImagePreload(deserialized.FilePath);
                        }
                        break;
                    }
                    case nameof(SetHotkeysMessage):
                    {
                        var deserialized = SetHotkeysMessage.Deserialize(message);
                        if (deserialized == null) break;
                        lock (_remoteWindows)
                        {
                            if (_remoteWindows.TryGetValue(deserialized.WindowId, out var w))
                                w.SetWatchedKeys(deserialized.ImGuiKeys);
                        }
                        break;
                    }
                    case nameof(RequestStyleDataMessage):
                    {
                        var deserialized = RequestStyleDataMessage.Deserialize(message);
                        if (deserialized == null) break;
                        lock (_remoteWindows)
                        {
                            if (_remoteWindows.TryGetValue(deserialized.WindowId, out var window))
                                window.HandleStyleDataRequest(deserialized.ThemeIndex, deserialized.RequestId);
                        }
                        break;
                    }
                    case nameof(FocusGameWindowMessage):
                    {
                        var deserialized = FocusGameWindowMessage.Deserialize(message);
                        if (deserialized == null) break;
                        lock (_remoteWindows)
                        {
                            if (_remoteWindows.TryGetValue(deserialized.WindowId, out var window))
                                window.QueueFocusGameWindow();
                        }
                        break;
                    }
                    case nameof(FocusOverlayWindowMessage):
                    {
                        var deserialized = FocusOverlayWindowMessage.Deserialize(message);
                        if (deserialized == null) break;
                        lock (_remoteWindows)
                        {
                            if (_remoteWindows.TryGetValue(deserialized.WindowId, out var window))
                                window.QueueFocusOverlayWindow();
                        }
                        break;
                    }
                    case nameof(GameShutDownMessage):
                    {
                        ShouldClose = true;
                        break;
                    }
                    default:
                    {
                        PluginManager?.TryHandleMessage(message);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHandler(ex.Message + "\n" + ex.StackTrace, IpcChannel.LogType.Error);
            }
        }
    }
}
