using System;
using System.Collections.Concurrent;

namespace lstwoMODS_Core;

public static class MainThread
{
    public static ConcurrentQueue<Action> Queue { get; } = new();

    public static void Enqueue(Action action)
    {
        Queue.Enqueue(action);
    }
}