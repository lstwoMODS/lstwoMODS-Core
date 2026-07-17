using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BepInEx;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace lstwoMODS_Core;

public static class DataStorage
{
    private static readonly string BaseDir =
        Path.Combine(Paths.GameRootPath, "lstwoMODS");

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();


    private static string ResolvePath(string id, string key)
    {
        // key supports forward-slash subfolders: "profiles/default"
        var relative = key.Replace('/', Path.DirectorySeparatorChar);
        var path = Path.Combine(BaseDir, id, relative + ".json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        return path;
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
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, json);
        if (File.Exists(path))
            File.Replace(tmp, path, path + ".bak");
        else
            File.Move(tmp, path);
    }


    /// <summary>Deserialize and return the saved value, or <c>default</c> if the file does not exist.</summary>
    public static T Load<T>(string id, string key)
    {
        var path = ResolvePath(id, key);
        if (File.Exists(path))
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[DataStorage] Load failed ({path}): {ex.Message}");
            }
        }

        // Main file missing or corrupt: fall back to the backup left by WriteAtomic.
        var bak = path + ".bak";
        if (!File.Exists(bak))
            return default;
        try
        {
            var value = JsonConvert.DeserializeObject<T>(File.ReadAllText(bak));
            Plugin.LogSource.LogWarning($"[DataStorage] Loaded from backup ({bak}).");
            return value;
        }
        catch (Exception ex)
        {
            Plugin.LogSource.LogWarning($"[DataStorage] Backup load failed ({bak}): {ex.Message}");
            return default;
        }
    }

    /// <summary>
    /// Serialize <paramref name="value"/> immediately (snapshot), then write to disk on a
    /// background thread. Concurrent saves to the same file are queued and written in order.
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
            Plugin.LogSource.LogWarning($"[DataStorage] Serialize failed ({path}): {ex.Message}");
            return;
        }

        var sem = LockFor(path);
        Task.Run(async () =>
        {
            await sem.WaitAsync();
            try
            {
                WriteAtomic(path, json);
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[DataStorage] Write failed ({path}): {ex.Message}");
            }
            finally
            {
                sem.Release();
            }
        });
    }

    /// <returns><c>true</c> if a saved file exists for this id + key.</returns>
    public static bool Exists(string id, string key)
        => File.Exists(ResolvePath(id, key));

    /// <summary>
    /// Enumerate the keys (file-name stems, no <c>.json</c>) of every file saved directly
    /// under <paramref name="subfolder"/> for this id. Used for one-file-per-item stores
    /// (e.g. macro groups): dropping a shared file into the folder makes it appear here.
    /// Order is filesystem-defined; sort at the call site. Backup/temp files are skipped.
    /// </summary>
    public static IReadOnlyList<string> ListKeys(string id, string subfolder)
    {
        var dir = Path.Combine(BaseDir, id, subfolder.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(dir)) return Array.Empty<string>();
        try
        {
            return Directory.GetFiles(dir, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .ToList();
        }
        catch (Exception ex)
        {
            Plugin.LogSource.LogWarning($"[DataStorage] ListKeys failed ({dir}): {ex.Message}");
            return Array.Empty<string>();
        }
    }

    /// <summary>Absolute path of the file backing this id + key. Useful for telling the user
    /// where a shareable file lives (creating any missing parent folders as a side effect).</summary>
    public static string GetFilePath(string id, string key) => ResolvePath(id, key);

    /// <summary>Deletes the saved file for this id + key if it exists.</summary>
    public static void Delete(string id, string key)
    {
        var path = ResolvePath(id, key);
        if (File.Exists(path))
            File.Delete(path);
    }

    // ── Bag API (one shared data.json per id, stored as a dict) ──────────

    // Lazy<T> inside GetOrAdd guarantees the disk load runs exactly once per id
    // even when multiple threads race on first access.
    private static readonly ConcurrentDictionary<string, Lazy<Dictionary<string, JToken>>> _bags = new();

    private static string BagPath(string id)
    {
        var dir = Path.Combine(BaseDir, id);
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "data.json");
    }

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
                    Plugin.LogSource.LogWarning($"[DataStorage] Bag load failed ({i}): {ex.Message}");
                }
            }

            // Main file missing or corrupt: fall back to the backup left by WriteAtomic.
            var bak = path + ".bak";
            if (!File.Exists(bak)) return new();
            try
            {
                var bag = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(
                    File.ReadAllText(bak));
                Plugin.LogSource.LogWarning($"[DataStorage] Bag loaded from backup ({i}).");
                return bag ?? new();
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[DataStorage] Bag backup load failed ({i}): {ex.Message}");
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
                Plugin.LogSource.LogWarning($"[DataStorage] Bag deserialize failed ({id}/{key}): {ex.Message}");
                return default;
            }
        }
    }

    /// <summary>
    /// Update <paramref name="key"/> in the shared bag and write <c>data.json</c> asynchronously.
    /// </summary>
    public static void SaveToBag<T>(string id, string key, T value)
    {
        JToken token;
        try { token = JToken.FromObject(value); }
        catch (Exception ex)
        {
            Plugin.LogSource.LogWarning($"[DataStorage] Bag serialize failed ({id}/{key}): {ex.Message}");
            return;
        }

        var bag = GetBag(id);
        string json;
        lock (bag)
        {
            bag[key] = token;
            try { json = JsonConvert.SerializeObject(bag, Formatting.Indented); }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[DataStorage] Bag serialize failed ({id}): {ex.Message}");
                return;
            }
        }

        var path = BagPath(id);
        var sem = LockFor(path);
        Task.Run(async () =>
        {
            await sem.WaitAsync();
            try { WriteAtomic(path, json); }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[DataStorage] Bag write failed ({id}): {ex.Message}");
            }
            finally { sem.Release(); }
        });
    }

    /// <summary>Remove <paramref name="key"/> from the shared bag and rewrite <c>data.json</c>.</summary>
    public static void DeleteFromBag(string id, string key)
    {
        var bag = GetBag(id);
        string json;
        lock (bag)
        {
            if (!bag.Remove(key)) return;
            try { json = JsonConvert.SerializeObject(bag, Formatting.Indented); }
            catch { return; }
        }

        var path = BagPath(id);
        var sem = LockFor(path);
        Task.Run(async () =>
        {
            await sem.WaitAsync();
            try { WriteAtomic(path, json); }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[DataStorage] Bag write failed ({id}): {ex.Message}");
            }
            finally { sem.Release(); }
        });
    }
}
