using System;

namespace lstwoMODS_Core.UI;

public class Ref<T>
{
    private T _value;

    /// <summary>
    /// Where <see cref="Changed"/> is raised.
    ///
    /// True (the default) means Unity's main thread. A change made on a background thread (the
    /// IPC reader thread, for anything the user did in the overlay) is queued and raised on the
    /// next <c>Plugin.Update</c>, so handlers can call Unity APIs without marshalling. A change
    /// already made on the main thread is raised inline, with no frame of latency.
    ///
    /// False raises it synchronously on whichever thread set <see cref="Value"/>, and only there:
    /// it is never also raised on the main thread. Use it for handlers that must keep working
    /// while the game is frozen or loading (the main thread is not ticking then) and that touch
    /// no Unity API. This mirrors the <c>mainThread</c> flag on UI elements.
    /// </summary>
    public bool RunCallbacksOnMainThread { get; set; } = true;

    /// <summary>
    /// Fired whenever Value is set, including when it is set to the value it already had.
    /// See <see cref="RunCallbacksOnMainThread"/> for which thread it arrives on.
    /// </summary>
    public event Action<T> Changed;

    public T Value
    {
        get => _value;
        set
        {
            _value = value;

            // Snapshot: with main-thread dispatch the handler list could otherwise change
            // between queueing and running.
            var handler = Changed;
            if (handler == null)
                return;

            if (RunCallbacksOnMainThread)
                MainThread.Invoke(() => handler(value));
            else
                handler(value);
        }
    }

    public Ref(T value = default)
    {
        _value = value;
    }

    /// <param name="runCallbacksOnMainThread">See <see cref="RunCallbacksOnMainThread"/>.</param>
    public Ref(T value, bool runCallbacksOnMainThread) : this(value)
    {
        RunCallbacksOnMainThread = runCallbacksOnMainThread;
    }

    /// <summary>Chainable form of <see cref="RunCallbacksOnMainThread"/>.</summary>
    public Ref<T> WithMainThreadCallbacks(bool value = true)
    {
        RunCallbacksOnMainThread = value;
        return this;
    }
}
