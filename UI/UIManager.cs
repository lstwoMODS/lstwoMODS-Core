using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.Messages;
using UnityEngine;
using Random = UnityEngine.Random;

namespace lstwoMODS_Core.UI;

public static class UIManager
{
    public static Action OnConfigure;
    public static Action OnRender;
    public static int MainIpcChannelPort;
    public static Process Process;
    public static IpcChannel IpcChannel;

    private static ConfigEntry<string> _overlayExePath;
    private static Task IpcChannelMainTask;

    public static Action OnInitialized;
    
    public static int GetRandomFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        listener.Stop();
        return port;
    }

    internal static void Initialize()
    {
        _overlayExePath = Plugin.ConfigFile.Bind(
            "Internal", 
            "Relative Overlay Exe File Path",
            "Overlay/lstwoMODS_Overlay.exe",
            "The relative path to the overlay exe file from the dll file."
        );
        
        MainIpcChannelPort = GetRandomFreePort();
        
        var exePath = Path.Combine(Path.GetDirectoryName(typeof(UIManager).Assembly.Location), _overlayExePath.Value);

        if (!File.Exists(exePath))
        {
            exePath = Path.Combine(Path.GetDirectoryName(typeof(UIManager).Assembly.Location), "Overlay/lstwoMODS_Overlay.exe");
            _overlayExePath.Value = "Overlay/lstwoMODS_Overlay.exe";
        }
        
        var gameProcessId = Process.GetCurrentProcess().Id;
        
        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = $"{MainIpcChannelPort} {gameProcessId}",
            WorkingDirectory = Path.GetDirectoryName(exePath),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        Process = new Process { StartInfo = psi };

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
        
        IpcChannel = new IpcChannel(true, MainIpcChannelPort, OnInitialized);

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

    internal static void Dispose()
    {
        IpcChannel.SendAndWaitAsync(new GameShutDownMessage().Serialize()).Wait();
        IpcChannel.Dispose();
    }
}