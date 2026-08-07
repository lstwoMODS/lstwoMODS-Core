using System.Diagnostics;

namespace lstwoMODS_Overlay;

/// <summary>
/// Rate-limited exception reporting for per-frame code paths. An error that recurs every
/// frame (60+/s) would otherwise flood the log: the first occurrence logs the full stack,
/// identical repeats are only counted and summarized once per interval.
/// </summary>
public static class CrashGuard
{
    private const long SummaryIntervalMs = 5000;
    private const int  MaxTrackedErrors  = 256;

    private static readonly Stopwatch _clock = Stopwatch.StartNew();
    private static readonly object _lock = new();
    private static readonly Dictionary<string, Entry> _entries = new();

    private class Entry
    {
        public long LastLogMs;
        public int  Suppressed;
    }

    public static void Report(string context, Exception ex)
    {
        var key = $"{context}|{ex.GetType().FullName}|{ex.Message}";

        lock (_lock)
        {
            if (!_entries.TryGetValue(key, out var entry))
            {
                // Distinct error signatures are naturally bounded, but a message containing
                // per-frame data (an index, a coordinate) could grow the map without limit.
                if (_entries.Count >= MaxTrackedErrors)
                    _entries.Clear();

                _entries[key] = new Entry { LastLogMs = _clock.ElapsedMilliseconds };
                Logger.LogError($"[CrashGuard] Exception in {context}: {ex}");
                return;
            }

            entry.Suppressed++;
            var now = _clock.ElapsedMilliseconds;
            if (now - entry.LastLogMs < SummaryIntervalMs)
                return;

            Logger.LogError(
                $"[CrashGuard] Exception in {context} repeated {entry.Suppressed}x in the last " +
                $"{(now - entry.LastLogMs) / 1000.0:0.#}s: {ex.GetType().Name}: {ex.Message}");
            entry.LastLogMs  = now;
            entry.Suppressed = 0;
        }
    }
}
