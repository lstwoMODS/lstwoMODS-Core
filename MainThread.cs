using System;
using System.Collections.Concurrent;
using System.Threading;

namespace lstwoMODS_Core;

/// <summary>
/// Hands work back to Unity's main thread. Anything driven by the UI arrives on the IPC reader
/// thread (the overlay is a separate process), and Unity APIs must not be touched from there.
/// The queue is drained several times per <c>Plugin.Update</c>.
/// </summary>
public static class MainThread
{
    private const int Unknown = -1;

    // volatile: background threads read this to decide whether to dispatch, and must not
    // keep seeing Unknown after Claim() has run on the main thread.
    private static volatile int _mainThreadId = Unknown;

    public static ConcurrentQueue<Action> Queue { get; } = new();

    /// <summary>True when the calling thread is Unity's main thread.</summary>
    public static bool IsMainThread => _mainThreadId == Thread.CurrentThread.ManagedThreadId;

    /// <summary>Record the calling thread as the main one. Called from <c>Plugin.Awake</c>.</summary>
    internal static void Claim() => _mainThreadId = Thread.CurrentThread.ManagedThreadId;

    /// <summary>Queue <paramref name="action"/> to run on the next <c>Plugin.Update</c>.</summary>
    public static void Enqueue(Action action)
    {
        Queue.Enqueue(action);
    }

    /// <summary>
    /// Run <paramref name="action"/> on the main thread, inline when we are already on it so a
    /// main-thread caller doesn't pay a frame of latency. Also runs inline before the main thread
    /// has been claimed, since queued work would have nothing to drain it.
    /// </summary>
    public static void Invoke(Action action)
    {
        if (_mainThreadId == Unknown || IsMainThread)
            action();
        else
            Queue.Enqueue(action);
    }

    /// <summary>
    /// Run everything queued so far. Each action is isolated: one that throws is logged and the
    /// rest still run, because the queue carries unrelated work from every mod and a single bad
    /// callback would otherwise abort the whole Update (and do it again every frame).
    /// </summary>
    internal static void Drain()
    {
        while (Queue.TryDequeue(out var action))
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"[MainThread] Queued action threw: {ex}");
            }
        }
    }
}
