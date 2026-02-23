
using System.Diagnostics;
using System.Net;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.Messages;

namespace lstwoMODS_Overlay
{
    public class Program
    {
        public static bool ShouldClose = false;
        public static int MainIpcChannelPort;
        public static IpcChannel IpcChannel;
        
        private static bool _isChannelInitialized = false;
        private static List<RemoteImGuiWindow> _remoteWindows = new();

        static void Main(string[] args)
        {
            Logger.Log("lstwoMODS Overlay starting...");

            try
            {
                MainIpcChannelPort = int.Parse(args[0]);
                
                IpcChannel = new IpcChannel(false, MainIpcChannelPort, () => _isChannelInitialized = true);
                IpcChannel.MessageReceived += MessageReceived;
                IpcChannel.Log += LogHandler;
                _ = IpcChannel.Main();
                
                while (!ShouldClose)
                {
                    Thread.Sleep(100);
                }
                
                IpcChannel.Dispose();

                Logger.Log("Overlay shut down successfully");
            }
            catch (Exception ex)
            {
                Logger.Log($"Fatal error in overlay: {ex.Message}");
                Logger.Log($"Stack trace: {ex.StackTrace}");
                Console.ReadLine();
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
                            deserialized.Width, deserialized.Height, type: deserialized.WindowType, iconPath: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.png"));
                        window.Elements = deserialized.Elements.ToList();
                        window.StartThread();
                        Console.WriteLine(deserialized.FollowWindowHandle);
                        Console.WriteLine(new IntPtr(deserialized.FollowWindowHandle));
                        window.TrackWindow(new IntPtr(deserialized.FollowWindowHandle));

                        lock (_remoteWindows)
                        {
                            _remoteWindows.Add(window);
                        }

                        break;
                    }
                    case nameof(GameShutDownMessage):
                    {
                        ShouldClose = true;
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
