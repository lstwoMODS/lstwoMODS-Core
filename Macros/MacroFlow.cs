using System;
using System.Collections;
using UnityEngine;

namespace lstwoMODS_Core.Macros;

/// <summary>
/// Stop-aware wait helpers for plugin-authored macro steps whose <c>Execute</c> returns an
/// <see cref="IEnumerator"/>. Yielding <c>new WaitForSeconds(...)</c> from such a step ignores the
/// editor Stop button (the wait runs to completion before the run notices it was stopped); these
/// poll <see cref="MacroCallChain.Stopped"/> every frame instead, so Stop lands within a frame.
///
/// Call these from inside <c>Execute</c> and return the result, the chain is captured at that
/// moment (like <c>core.wait</c>), because <see cref="MacroRunner.CurrentChain"/> is only set while
/// the runner is inside a step's <c>Execute</c>, not once the routine is being iterated.
/// </summary>
public static class MacroFlow
{
    /// <summary>Wait <paramref name="seconds"/> of scaled game time, ending early if the run is stopped.</summary>
    public static IEnumerator Wait(float seconds) => WaitRoutine(seconds, MacroRunner.CurrentChain);

    /// <summary>Wait <paramref name="frames"/> rendered frames, ending early if the run is stopped.</summary>
    public static IEnumerator WaitFrames(int frames) => WaitFramesRoutine(frames, MacroRunner.CurrentChain);

    /// <summary>Yield each frame until <paramref name="predicate"/> is true (or the run is stopped).
    /// A null predicate returns immediately.</summary>
    public static IEnumerator WaitUntil(Func<bool> predicate) => WaitWhileRoutine(predicate, want: true, MacroRunner.CurrentChain);

    /// <summary>Yield each frame while <paramref name="predicate"/> is true (or until the run is stopped).
    /// A null predicate returns immediately.</summary>
    public static IEnumerator WaitWhile(Func<bool> predicate) => WaitWhileRoutine(predicate, want: false, MacroRunner.CurrentChain);

    private static IEnumerator WaitRoutine(float seconds, MacroCallChain chain)
    {
        for (var elapsed = 0f; elapsed < seconds; elapsed += Time.deltaTime)
        {
            if (chain != null && chain.Stopped) yield break;
            yield return null;
        }
    }

    private static IEnumerator WaitFramesRoutine(int frames, MacroCallChain chain)
    {
        for (var i = 0; i < frames; i++)
        {
            if (chain != null && chain.Stopped) yield break;
            yield return null;
        }
    }

    /// <summary>Shared body for WaitUntil/WaitWhile: loop while the predicate differs from
    /// <paramref name="want"/> (Until wants true, While wants the predicate to stay true).</summary>
    private static IEnumerator WaitWhileRoutine(Func<bool> predicate, bool want, MacroCallChain chain)
    {
        if (predicate == null) yield break;
        while (predicate() != want)
        {
            if (chain != null && chain.Stopped) yield break;
            yield return null;
        }
    }
}
