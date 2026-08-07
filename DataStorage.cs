using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using BepInEx;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace lstwoMODS_Core;

/// <summary>
/// JSON-backed persistence for mods, with write-behind caching.
///
/// Saves do not touch the disk directly. They update the in-memory state and mark the target
/// file dirty; a background flusher writes it once things go quiet. This matters because
/// settings without an apply button fire their change callback on <b>every</b> frame the widget
/// value differs, so dragging a slider for a few seconds used to mean a few hundred
/// <see cref="File.Replace(string,string,string)"/> calls in the game directory, each one going
/// through the antivirus filter stack. Coalescing also makes the last change win by
/// construction, which the old fire-and-forget <c>Task.Run</c> per save could not guarantee.
///
/// Readers never see stale data: <see cref="Load{T}"/>, <see cref="Exists"/>,
/// <see cref="ListKeys"/> and <see cref="GetFilePath"/> all account for writes still in flight,
/// and <see cref="FlushAll"/> runs on shutdown. The only loss window is a hard crash or kill
/// within <see cref="MaxDelayMs"/> of a change, which is an acceptable trade for settings.
/// </summary>
public static class DataStorage
{
    /// <summary>Write a file this long after the last change to it.</summary>
    private const int DebounceMs = 500;
    /// <summary>...but never hold a change longer than this, however busy the file is.</summary>
    private const int MaxDelayMs = 3000;
    /// <summary>How often the flusher re-checks while anything is pending.</summary>
    private const int TickMs = 100;

    private static readonly string BaseDir =
        Path.Combine(Paths.GameRootPath, "lstwoMODS");

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    /// <summary>Files with unwritten changes, keyed by absolute path.</summary>
    private static readonly Dictionary<string, PendingWrite> _pending = new();
    private static readonly object _pendingLock = new();

    /// <summary>Last JSON actually written per path, so a no-op save doesn't hit the disk at all.</summary>
    private static readonly ConcurrentDictionary<string, string> _lastWritten = new();

    private static readonly Stopwatch _clock = Stopwatch.StartNew();
    private static readonly AutoResetEvent _flushSignal = new(false);
    private static readonly object _flusherLock = new();
    private static Thread _flusher;

    /// <summary>Logging that cannot itself throw: the flusher also runs during shutdown,
    /// after the plugin instance backing <see cref="Plugin.LogSource"/> may be gone.</summary>
    private static void Warn(string message)
    {
        try { Plugin.LogSource.LogWarning(message); }
        catch { /* nothing useful left to report it to */ }
    }

    private sealed class PendingWrite
    {
        /// <summary>Eagerly serialized snapshot, for values the caller may mutate after saving.</summary>
        public string Json;
        /// <summary>Deferred serializer, for state we own and can serialize once at flush time.</summary>
        public Func<string> Producer;
        public long FirstDirtyMs;
        public long LastChangeMs;
    }


    private static string ResolvePath(string id, string key)
    {
        // key supports forward-slash subfolders: "profiles/default"
        var relative = key.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(BaseDir, id, relative + ".json");
    }

    private static SemaphoreSlim LockFor(string path)
        => _locks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));

    /// <summary>
    /// Crash-safe write: write to a temp file, then swap it into place. When the
    /// destination exists, <see cref="File.Replace(string,string,string)"/> also keeps
    /// the previous version as <c>.bak</c>, which the loaders fall back to.
    /// </summary>
    private static void WriteAtomic(string path, string json)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var tmp = path + ".tmp";
        File.WriteAllText(tmp, json);
        if (File.Exists(path))
            File.Replace(tmp, path, path + ".bak");
        else
            File.Move(tmp, path);
    }


    // ── Write-behind scheduling ─────────────────────────────────────────

    /// <summary>Mark <paramref name="path"/> dirty. Supply either a snapshot or a deferred serializer.</summary>
    private static void QueueWrite(string path, string json, Func<string> producer)
    {
        var now = _clock.ElapsedMilliseconds;

        lock (_pendingLock)
        {
            if (!_pending.TryGetValue(path, out var pending))
                _pending[path] = pending = new PendingWrite { FirstDirtyMs = now };

            // Superseding whatever was queued before is the point: the newest value wins,
            // and a burst of changes collapses into a single write.
            pending.Json = json;
            pending.Producer = producer;
            pending.LastChangeMs = now;
        }

        EnsureFlusher();
        _flushSignal.Set();
    }

    private static void EnsureFlusher()
    {
        if (_flusher != null)
            return;

        lock (_flusherLock)
        {
            if (_flusher != null)
                return;

            // Backstop for shutdown paths that never reach Plugin.OnDestroy.
            AppDomain.CurrentDomain.ProcessExit += (_, _) => FlushAll();

            _flusher = new Thread(FlushLoop)
            {
                IsBackground = true,
                Name = "lstwoMODS DataStorage flusher",
            };
            _flusher.Start();
        }
    }

    private static void FlushLoop()
    {
        while (true)
        {
            bool idle;
            lock (_pendingLock) idle = _pending.Count == 0;

            // Sleep outright while there is nothing to do; QueueWrite wakes us. The signal is
            // an AutoResetEvent, so a Set() that lands between the check and the wait is not lost.
            if (idle)
                _flushSignal.WaitOne();
            else
                _flushSignal.WaitOne(TickMs);

            try
            {
                FlushDue();
            }
            catch (Exception ex)
            {
                Warn($"[DataStorage] Flush failed: {ex.Message}");
            }
        }
    }

    /// <summary>Write every pending file that has gone quiet, or that has waited long enough.</summary>
    private static void FlushDue()
    {
        var now = _clock.ElapsedMilliseconds;
        List<string> due = null;

        lock (_pendingLock)
        {
            foreach (var pair in _pending)
            {
                var pending = pair.Value;

                if (now - pending.LastChangeMs < DebounceMs && now - pending.FirstDirtyMs < MaxDelayMs)
                    continue;

                (due ??= new List<string>()).Add(pair.Key);
            }
        }

        if (due == null)
            return;

        foreach (var path in due)
            WritePending(path);
    }

    /// <summary>Write one pending file now. No-op when nothing is pending for it.</summary>
    private static void WritePending(string path)
    {
        PendingWrite pending;

        lock (_pendingLock)
        {
            if (!_pending.TryGetValue(path, out pending))
                return;

            _pending.Remove(path);
        }

        string json;
        try
        {
            json = pending.Producer != null ? pending.Producer() : pending.Json;
        }
        catch (Exception ex)
        {
            Warn($"[DataStorage] Serialize failed ({path}): {ex.Message}");
            return;
        }

        if (json == null)
            return;

        var sem = LockFor(path);
        sem.Wait();
        try
        {
            if (_lastWritten.TryGetValue(path, out var previous) && previous == json && File.Exists(path))
                return;

            WriteAtomic(path, json);
            _lastWritten[path] = json;
        }
        catch (Exception ex)
        {
            Warn($"[DataStorage] Write failed ({path}): {ex.Message}");
        }
        finally
        {
            sem.Release();
        }
    }

    /// <summary>Drop a queued write, e.g. because the file was just deleted.</summary>
    private static void DiscardPending(string path)
    {
        lock (_pendingLock) _pending.Remove(path);
        _lastWritten.TryRemove(path, out _);
    }

    private static string PeekPending(string path)
    {
        lock (_pendingLock)
            return _pending.TryGetValue(path, out var pending) ? pending.Json : null;
    }

    private static bool HasPending(string path)
    {
        lock (_pendingLock) return _pending.ContainsKey(path);
    }

    /// <summary>Write out anything pending directly inside <paramref name="dir"/>, so a directory listing is complete.</summary>
    private static void FlushDirectory(string dir)
    {
        List<string> paths = null;

        lock (_pendingLock)
        {
            foreach (var path in _pending.Keys)
            {
                if (string.Equals(Path.GetDirectoryName(path), dir, StringComparison.OrdinalIgnoreCase))
                    (paths ??= new List<string>()).Add(path);
            }
        }

        if (paths == null)
            return;

        foreach (var path in paths)
            WritePending(path);
    }

    /// <summary>Write the pending change for this id + key, if any, before this call returns.</summary>
    public static void Flush(string id, string key) => WritePending(ResolvePath(id, key));

    /// <summary>Write the pending shared <c>data.json</c> for this id, if any, before this call returns.</summary>
    public static void FlushBag(string id) => WritePending(BagPath(id));

    /// <summary>
    /// Write every pending file before returning. Called on shutdown; also safe to call
    /// whenever durability matters right now (before sharing a file, before a risky operation).
    /// </summary>
    public static void FlushAll()
    {
        List<string> paths;
        lock (_pendingLock) paths = _pending.Keys.ToList();

        foreach (var path in paths)
            WritePending(path);
    }


    /// <summary>Deserialize and return the saved value, or <c>default</c> if the file does not exist.</summary>
    public static T Load<T>(string id, string key)
    {
        var path = ResolvePath(id, key);

        // Read-your-writes: a value saved moments ago may not be on disk yet.
        var pending = PeekPending(path);
        if (pending != null)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(pending);
            }
            catch (Exception ex)
            {
                Warn($"[DataStorage] Pending load failed ({path}): {ex.Message}");
            }
        }

        if (File.Exists(path))
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                Warn($"[DataStorage] Load failed ({path}): {ex.Message}");
            }
        }

        // Main file missing or corrupt: fall back to the backup left by WriteAtomic.
        var bak = path + ".bak";
        if (!File.Exists(bak))
            return default;
        try
        {
            var value = JsonConvert.DeserializeObject<T>(File.ReadAllText(bak));
            Warn($"[DataStorage] Loaded from backup ({bak}).");
            return value;
        }
        catch (Exception ex)
        {
            Warn($"[DataStorage] Backup load failed ({bak}): {ex.Message}");
            return default;
        }
    }

    /// <summary>
    /// Serialize <paramref name="value"/> immediately (snapshot), then write to disk once the
    /// file goes quiet. Repeated saves to the same key coalesce; the newest value wins.
    /// </summary>
    public static void Save<T>(string id, string key, T value)
    {
        var path = ResolvePath(id, key);

        // Serialize synchronously so the caller's object can safely mutate after this returns.
        string json;
        try
        {
            json = JsonConvert.SerializeObject(value, Formatting.Indented);
        }
        catch (Exception ex)
        {
            Warn($"[DataStorage] Serialize failed ({path}): {ex.Message}");
            return;
        }

        QueueWrite(path, json, null);
    }

    /// <returns><c>true</c> if a saved file exists for this id + key.</returns>
    public static bool Exists(string id, string key)
    {
        var path = ResolvePath(id, key);
        return HasPending(path) || File.Exists(path);
    }

    /// <summary>
    /// Enumerate the keys (file-name stems, no <c>.json</c>) of every file saved directly
    /// under <paramref name="subfolder"/> for this id. Used for one-file-per-item stores
    /// (e.g. macro groups): dropping a shared file into the folder makes it appear here.
    /// Order is filesystem-defined; sort at the call site. Backup/temp files are skipped.
    /// </summary>
    public static IReadOnlyList<string> ListKeys(string id, string subfolder)
    {
        var dir = Path.Combine(BaseDir, id, subfolder.Replace('/', Path.DirectorySeparatorChar));

        // A just-saved item must show up in its own listing.
        FlushDirectory(dir);

        if (!Directory.Exists(dir)) return Array.Empty<string>();
        try
        {
            return Directory.GetFiles(dir, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .ToList();
        }
        catch (Exception ex)
        {
            Warn($"[DataStorage] ListKeys failed ({dir}): {ex.Message}");
            return Array.Empty<string>();
        }
    }

    /// <summary>Absolute path of the file backing this id + key. Useful for telling the user
    /// where a shareable file lives (creating any missing parent folders as a side effect).
    /// Any pending write is flushed first, so the file on disk is current when handed out.</summary>
    public static string GetFilePath(string id, string key)
    {
        var path = ResolvePath(id, key);
        WritePending(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        return path;
    }

    /// <summary>Deletes the saved file for this id + key if it exists.</summary>
    public static void Delete(string id, string key)
    {
        var path = ResolvePath(id, key);

        // Drop the queued write first, or the flusher would resurrect the file.
        DiscardPending(path);

        if (File.Exists(path))
            File.Delete(path);
    }

    // ── Bag API (one shared data.json per id, stored as a dict) ──────────

    // Lazy<T> inside GetOrAdd guarantees the disk load runs exactly once per id
    // even when multiple threads race on first access.
    private static readonly ConcurrentDictionary<string, Lazy<Dictionary<string, JToken>>> _bags = new();

    private static string BagPath(string id) => Path.Combine(BaseDir, id, "data.json");

    private static Dictionary<string, JToken> GetBag(string id) =>
        _bags.GetOrAdd(id, i => new Lazy<Dictionary<string, JToken>>(() =>
        {
            var path = BagPath(i);
            if (File.Exists(path))
            {
                try
                {
                    return JsonConvert.DeserializeObject<Dictionary<string, JToken>>(
                        File.ReadAllText(path)) ?? new();
                }
                catch (Exception ex)
                {
                    Warn($"[DataStorage] Bag load failed ({i}): {ex.Message}");
                }
            }

            // Main file missing or corrupt: fall back to the backup left by WriteAtomic.
            var bak = path + ".bak";
            if (!File.Exists(bak)) return new();
            try
            {
                var bag = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(
                    File.ReadAllText(bak));
                Warn($"[DataStorage] Bag loaded from backup ({i}).");
                return bag ?? new();
            }
            catch (Exception ex)
            {
                Warn($"[DataStorage] Bag backup load failed ({i}): {ex.Message}");
                return new();
            }
        })).Value;

    /// <returns><c>true</c> if <paramref name="key"/> exists in the shared bag for <paramref name="id"/>.</returns>
    public static bool BagEntryExists(string id, string key)
    {
        var bag = GetBag(id);
        lock (bag) return bag.ContainsKey(key);
    }

    /// <summary>Load a value from the shared bag, or <c>default</c> if the key is absent.</summary>
    public static T LoadFromBag<T>(string id, string key)
    {
        var bag = GetBag(id);
        lock (bag)
        {
            if (!bag.TryGetValue(key, out var token)) return default;
            try { return token.ToObject<T>(); }
            catch (Exception ex)
            {
                Warn($"[DataStorage] Bag deserialize failed ({id}/{key}): {ex.Message}");
                return default;
            }
        }
    }

    /// <summary>
    /// Update <paramref name="key"/> in the shared bag; <c>data.json</c> is rewritten once the
    /// file goes quiet. The bag is authoritative in memory, so the serialize is deferred too and
    /// a burst of changes costs one serialize instead of one per change.
    /// </summary>
    public static void SaveToBag<T>(string id, string key, T value)
    {
        JToken token;
        try { token = JToken.FromObject(value); }
        catch (Exception ex)
        {
            Warn($"[DataStorage] Bag serialize failed ({id}/{key}): {ex.Message}");
            return;
        }

        var bag = GetBag(id);
        lock (bag) bag[key] = token;

        QueueWrite(BagPath(id), null, () => SerializeBag(id, bag));
    }

    /// <summary>Remove <paramref name="key"/> from the shared bag and rewrite <c>data.json</c>.</summary>
    public static void DeleteFromBag(string id, string key)
    {
        var bag = GetBag(id);
        lock (bag)
        {
            if (!bag.Remove(key)) return;
        }

        QueueWrite(BagPath(id), null, () => SerializeBag(id, bag));
    }

    private static string SerializeBag(string id, Dictionary<string, JToken> bag)
    {
        lock (bag)
        {
            try
            {
                return JsonConvert.SerializeObject(bag, Formatting.Indented);
            }
            catch (Exception ex)
            {
                Warn($"[DataStorage] Bag serialize failed ({id}): {ex.Message}");
                return null;
            }
        }
    }
}
