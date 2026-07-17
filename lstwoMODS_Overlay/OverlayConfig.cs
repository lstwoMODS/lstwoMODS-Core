using System.IO;

namespace lstwoMODS_Overlay;

public class OverlayConfig
{
    public string Backend { get; set; } = "opengl";

    private static readonly string ConfigPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "overlay.config");

    public static OverlayConfig Load()
    {
        var config = new OverlayConfig();

        if (!File.Exists(ConfigPath))
            return config;

        foreach (var rawLine in File.ReadAllLines(ConfigPath))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("#") || !line.Contains("="))
                continue;

            var idx = line.IndexOf('=');
            var key = line.Substring(0, idx).Trim().ToLowerInvariant();
            var val = line.Substring(idx + 1).Trim();

            if (key == "backend")
                config.Backend = val;
        }

        return config;
    }
}
