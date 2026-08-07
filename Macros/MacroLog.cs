using System;

namespace lstwoMODS_Core.Macros;

/// <summary>Severity for the <c>core.log</c> macro step; maps onto the BepInEx log levels.</summary>
public enum MacroLogLevel
{
    Info,
    Warning,
    Error,
    Debug,
}

/// <summary>
/// Logging indirection for the macro data-model classes. Keeps ValueSource/Macro
/// deserialization free of a direct Plugin (→ UnityEngine) type dependency so the
/// model can be loaded and round-tripped outside the game (test harnesses).
/// </summary>
internal static class MacroLog
{
    public static void Warn(string message)
    {
        try
        {
            WarnViaPlugin(message);
        }
        catch
        {
            Console.WriteLine("[WARN] " + message);
        }
    }

    private static void WarnViaPlugin(string message) => Plugin.LogSource.LogWarning(message);

    /// <summary>Emit a message at the given level, falling back to the console outside the game.</summary>
    public static void Write(MacroLogLevel level, string message)
    {
        try
        {
            WriteViaPlugin(level, message);
        }
        catch
        {
            Console.WriteLine($"[{level}] {message}");
        }
    }

    private static void WriteViaPlugin(MacroLogLevel level, string message)
    {
        switch (level)
        {
            case MacroLogLevel.Warning: Plugin.LogSource.LogWarning(message); break;
            case MacroLogLevel.Error:   Plugin.LogSource.LogError(message); break;
            case MacroLogLevel.Debug:   Plugin.LogSource.LogDebug(message); break;
            default:                    Plugin.LogSource.LogInfo(message); break;
        }
    }
}
