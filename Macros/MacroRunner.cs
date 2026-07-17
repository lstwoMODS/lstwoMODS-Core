using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace lstwoMODS_Core.Macros;

/// <summary>
/// Tracks the chain of macro-to-macro calls behind one run so the runner can catch
/// runaway loops: a macro may iterate within one frame, but re-entering it more than
/// <see cref="MacroRunner.MaxSameFrameCalls"/> times with no frame passing (nothing
/// yielded → it would hang the game) aborts with a clear error, as does nesting waited
/// calls too deep. A Wait anywhere in the cycle resets the budget every frame, so
/// long-running loops just spread across frames.
/// </summary>
public sealed class MacroCallChain
{
    internal readonly List<(string MacroId, string Name, int Frame)> Stack = new();
    internal int Depth;

    /// <summary>Shared by every run in one tree (waited sub-calls share the chain,
    /// fire-and-forget children inherit it), so stopping any macro in the tree winds
    /// down the whole thing, which is what ends a Wait-driven loop.</summary>
    internal MacroRunToken Token = new();

    /// <summary>Whether this run tree has been asked to stop. A plugin step that returns
    /// an IEnumerator should capture <see cref="MacroRunner.CurrentChain"/> inside its
    /// Execute and poll this each frame in the routine (like core.wait does) instead of
    /// yielding one long WaitForSeconds, so Stop cancels it promptly.</summary>
    public bool Stopped => Token.Stopped;
}

/// <summary>Cancellation flag plus which macros a run tree currently has live.</summary>
internal sealed class MacroRunToken
{
    public bool Stopped;
    /// <summary>Macro id → live routine count within this tree.</summary>
    public readonly Dictionary<string, int> Active = new();
}

/// <summary>
/// A macro sub-run wrapped so its caller can read the value the callee handed back via its Return
/// step. Yield it like any coroutine (the runner does); once it has finished,
/// <see cref="ReturnValue"/> holds the callee's <see cref="MacroRunContext.ReturnValue"/>. This is
/// how a waited Run Macro / If / Switch step produces the called macro's return value as its output.
/// </summary>
public sealed class MacroResultRoutine : IEnumerator
{
    private readonly IEnumerator _inner;
    private readonly MacroRunContext _ctx;

    internal MacroResultRoutine(IEnumerator inner, MacroRunContext ctx)
    {
        _inner = inner;
        _ctx = ctx;
    }

    /// <summary>The value the run returned (null until it has completed, or when it returned nothing).</summary>
    public object ReturnValue => _ctx?.ReturnValue;

    public object Current => _inner.Current;
    public bool MoveNext() => _inner.MoveNext();
    public void Reset() => _inner.Reset();
}

/// <summary>
/// Executes a macro's steps in order inside a main-thread coroutine. A failed step
/// aborts the rest of the run; a macro that is still running ignores new run requests.
/// </summary>
public static class MacroRunner
{
    private const int MaxNestedDepth = 64;

    /// <summary>How often one macro may run without a frame passing. Same-frame loops are
    /// allowed; this only catches the infinite ones (no Wait anywhere in the cycle, which
    /// would hang the game inside a single frame). Kept moderate because same-frame tail
    /// calls also recurse on the native stack via StartCoroutine.</summary>
    internal const int MaxSameFrameCalls = 200;

    private static readonly HashSet<string> _running = new();

    /// <summary>Live run trees, for <see cref="Stop"/> targeting.</summary>
    private static readonly List<MacroRunToken> _tokens = new();

    /// <summary>Macro id → live routine count across ALL trees. Unlike
    /// <see cref="_running"/> (root trigger dedup only) this stays non-zero for the whole
    /// life of a Wait-driven loop, where each iteration is a fresh non-root run.</summary>
    private static readonly Dictionary<string, int> _activeCounts = new();

    /// <summary>Fired (main thread) with the macro id when a macro starts running or all
    /// of its runs have ended; drives the editor's Play/Stop button.</summary>
    public static event Action<string> RunningChanged;

    /// <summary>The call chain of the step currently executing; non-null only while the
    /// runner is inside a step's Execute. The Run Macro builtin captures it to extend the
    /// chain into the macros it calls. Plugin methods that return an IEnumerator do the
    /// same: capture this in Execute, then poll <see cref="MacroCallChain.Stopped"/>
    /// inside the routine to stay cancellable.</summary>
    public static MacroCallChain CurrentChain { get; private set; }

    /// <summary>The run context of the step currently executing; non-null only while the runner is
    /// inside a step's Execute (set alongside <see cref="CurrentChain"/>). The Return builtin uses
    /// it to stop the run and stash a return value; custom steps reach it through their
    /// <see cref="CustomStepRunContext"/>.</summary>
    public static MacroRunContext CurrentContext { get; private set; }

    /// <summary>Fired (main thread) when a step throws. The editor uses this for error display.</summary>
    public static event Action<Macro, MacroStep, Exception> StepFailed;

    /// <summary>Fired (main thread) when a root run ends: the macro, whether it finished without a
    /// failing step, and the value it returned (its Return step's value, or null). Nested and
    /// detached sub-runs don't fire this  read a waited call's result via
    /// <see cref="MacroResultRoutine.ReturnValue"/> instead.</summary>
    public static event Action<Macro, bool, object> Completed;

    public static bool IsRunning(Macro macro) => macro != null && _activeCounts.ContainsKey(macro.Id);

    /// <summary>Whether a root run of <paramref name="macro"/> is live; the same check
    /// <see cref="Run"/> dedups on, so <see cref="MacroManager.Fire"/> can skip side
    /// effects (toggle advance) for a fire that Run would ignore.</summary>
    internal static bool IsRootRunning(Macro macro) => macro != null && _running.Contains(macro.Id);

    /// <summary>Wind down every run tree <paramref name="macro"/> is currently part of.
    /// Routines exit at their next step boundary; a core.wait in progress notices within
    /// a frame, so Wait-driven loops stop promptly.</summary>
    public static void Stop(Macro macro)
    {
        if (macro == null) return;
        foreach (var token in _tokens)
            if (token.Active.ContainsKey(macro.Id))
                token.Stopped = true;
    }

    /// <param name="offSteps">Run <see cref="Macro.OffSteps"/> instead of <see cref="Macro.Steps"/> (hotkey Toggle mode).</param>
    /// <param name="triggerValues">Named values the trigger fired with (which player, money gained,
    /// ...), surfaced to the run's expressions; null for a run with no trigger data.</param>
    public static void Run(Macro macro, bool offSteps = false, IReadOnlyDictionary<string, object> triggerValues = null)
    {
        if (macro == null || !macro.Enabled) return;
        if (!_running.Add(macro.Id)) return;
        var ctx = new MacroRunContext { TriggerValues = triggerValues ?? MacroRunContext.NoTrigger };
        Plugin._StartCoroutine(RunRoutine(macro, offSteps ? macro.OffSteps : macro.Steps, new MacroCallChain(), root: true, ctx));
    }

    /// <summary>Run <paramref name="macro"/> as a waited sub-call of the given chain: the
    /// caller yields the returned routine, so it completes before the next step. Ignores
    /// the Enabled flag and the already-running guard (an explicit call is a subroutine,
    /// not a trigger); the chain's loop guard is what stops runaway cycles.</summary>
    /// <param name="offSteps">Run the Off list; pass <see cref="MacroManager.AdvanceToggle"/>
    /// so the call behaves like every other way of firing a Toggle macro.</param>
    /// <returns>A <see cref="MacroResultRoutine"/>: yield it like any coroutine, then read
    /// <see cref="MacroResultRoutine.ReturnValue"/> for whatever the callee's Return step handed
    /// back. The runner does this automatically, so a waited Run Macro / If / Switch step's output
    /// is the called macro's return value.</returns>
    public static MacroResultRoutine RunNested(Macro macro, MacroCallChain chain, bool offSteps = false)
    {
        var ctx = new MacroRunContext();
        var routine = RunRoutine(macro, offSteps ? macro.OffSteps : macro.Steps, chain ?? new MacroCallChain(), root: false, ctx);
        return new MacroResultRoutine(routine, ctx);
    }

    /// <summary>Run <paramref name="macro"/> without waiting for it (its own coroutine).
    /// The new chain inherits the caller's same-frame entries, so a cycle with no yield in
    /// between still trips the loop guard, which throws out of this call, into the
    /// calling step. Entries from earlier frames can never match and are dropped.</summary>
    /// <param name="offSteps">Run the Off list; pass <see cref="MacroManager.AdvanceToggle"/>
    /// so the call behaves like every other way of firing a Toggle macro.</param>
    public static void RunDetached(Macro macro, MacroCallChain parent, bool offSteps = false)
    {
        if (macro == null || parent?.Token.Stopped == true) return;
        var chain = new MacroCallChain();
        if (parent != null)
        {
            chain.Token = parent.Token;
            foreach (var entry in parent.Stack)
                if (entry.Frame == Time.frameCount)
                    chain.Stack.Add(entry);
        }
        // Checked here, before the coroutine starts, so the loop error surfaces as the
        // calling step's failure (Unity swallows exceptions thrown inside coroutines).
        GuardChain(macro, chain);
        Plugin._StartCoroutine(RunRoutine(macro, offSteps ? macro.OffSteps : macro.Steps, chain, root: false, new MacroRunContext()));
    }

    /// <summary>Throws when entering <paramref name="macro"/> would exceed the same-frame
    /// iteration cap (a runaway loop) or the waited-call nesting cap.</summary>
    private static void GuardChain(Macro macro, MacroCallChain chain)
    {
        var frame = Time.frameCount;
        var sameFrame = 0;
        foreach (var entry in chain.Stack)
            if (entry.MacroId == macro.Id && entry.Frame == frame)
                sameFrame++;
        if (sameFrame >= MaxSameFrameCalls)
            throw new InvalidOperationException(
                $"Loop guard: macro '{macro.Name}' ran {MaxSameFrameCalls} times without a frame passing; "
                + "aborting a likely infinite loop. Put a Wait step in the cycle if it should keep going.");
        if (chain.Depth >= MaxNestedDepth)
            throw new InvalidOperationException(
                $"Macro calls nested more than {MaxNestedDepth} deep at '{macro.Name}'. "
                + "For loops, use Run Macro with wait off (a tail call) instead of waited self-calls.");
    }

    private static IEnumerator RunRoutine(Macro macro, List<MacroStep> steps, MacroCallChain chain, bool root, MacroRunContext ctx)
    {
        GuardChain(macro, chain);
        chain.Stack.Add((macro.Id, macro.Name, Time.frameCount));
        chain.Depth++;
        var token = chain.Token;
        token.Active.TryGetValue(macro.Id, out var live);
        token.Active[macro.Id] = live + 1;
        if (!_tokens.Contains(token)) _tokens.Add(token);
        _activeCounts.TryGetValue(macro.Id, out var total);
        _activeCounts[macro.Id] = total + 1;
        if (total == 0) RunningChanged?.Invoke(macro.Id);

        ctx.Macro = macro;
        // Group id for the variable-scope ambient; resolved once (GroupOf is a linear scan).
        var groupId = MacroManager.GroupOf(macro)?.Id;
        var runFailed = false; // read in the finally for the Completed event's success flag

        // try/finally so the bookkeeping above unwinds even when Unity destroys the
        // coroutine externally (scene teardown); a leak here bricks re-triggering.
        try
        {
            foreach (var step in steps ?? (IEnumerable<MacroStep>)Array.Empty<MacroStep>())
            {
                if (token.Stopped) break;

                object result = null;
                var failed = false;

                // Ambient current macro for Group/Macro variable scopes and the var() function,
                // plus this run's context for the trigger() function; both live across arg
                // evaluation and Execute (narrow scope, like CurrentChain, so a nested run's own
                // value doesn't leak back up). Restored per step.
                var prevScope = MacroVariables.Current;
                var prevAmbient = MacroRunContext.Ambient;
                MacroVariables.Current = (macro.Id, groupId);
                MacroRunContext.Ambient = ctx;
                try
                {
                    var method = MacroRegistry.Find(step.MethodId);
                    if (method == null)
                    {
                        if (!string.IsNullOrEmpty(step.MethodId))
                            Plugin.LogSource.LogWarning($"[Macros] '{macro.Name}': method '{step.MethodId}' not found; step skipped.");
                        continue;
                    }

                    if (method.ExecuteCustom != null)
                    {
                        CurrentChain = chain;
                        CurrentContext = ctx;
                        try { result = method.ExecuteCustom(new CustomStepRunContext(step, ctx, chain)); }
                        finally { CurrentChain = null; CurrentContext = null; }
                    }
                    else
                    {
                        step.MigrateLegacyArgs(method);

                        var args = new object[method.Parameters.Length];
                        for (int i = 0; i < args.Length; i++)
                        {
                            var param = method.Parameters[i];
                            var source = step.GetArg(param.Name);
                            args[i] = source != null ? source.Evaluate(param, ctx) : DefaultArgFor(param);
                        }

                        CurrentChain = chain;
                        CurrentContext = ctx;
                        try { result = method.Execute(args); }
                        finally { CurrentChain = null; CurrentContext = null; }
                    }
                }
                catch (Exception ex)
                {
                    Fail(macro, step, ex);
                    failed = true;
                }
                finally
                {
                    MacroVariables.Current = prevScope;
                    MacroRunContext.Ambient = prevAmbient;
                }

                if (!failed && result is IEnumerator nested)
                {
                    yield return Guarded(nested, token, ex => { Fail(macro, step, ex); failed = true; });
                    // A waited macro call carries its Return value out; any other coroutine (wait) has none.
                    result = nested is MacroResultRoutine rr ? rr.ReturnValue : null;
                }

                if (failed) { runFailed = true; break; }
                ctx.RecordOutput(step, result);
                if (ctx.Returned) break; // Return step asked to stop the rest of this macro
            }
        }
        finally
        {
            // Nested sub-runs push above us and pop before we resume, so our entry is last.
            chain.Stack.RemoveAt(chain.Stack.Count - 1);
            chain.Depth--;
            if (token.Active.TryGetValue(macro.Id, out live))
            {
                if (live <= 1) token.Active.Remove(macro.Id);
                else token.Active[macro.Id] = live - 1;
            }
            if (token.Active.Count == 0) _tokens.Remove(token);
            if (_activeCounts.TryGetValue(macro.Id, out total))
            {
                if (total <= 1)
                {
                    _activeCounts.Remove(macro.Id);
                    RunningChanged?.Invoke(macro.Id);
                }
                else
                {
                    _activeCounts[macro.Id] = total - 1;
                }
            }
            if (root)
            {
                _running.Remove(macro.Id);
                Completed?.Invoke(macro, !runFailed, ctx.ReturnValue);
            }
        }
    }

    /// <summary>Value for a parameter with no stored source: a registered macro type's
    /// parameterless default mode (e.g. Player → the local player, which is what steps
    /// saved before context params existed should target), else the type default.</summary>
    private static object DefaultArgFor(MacroParam param)
    {
        var mode = MacroTypes.For(param.Type)?.DefaultMode;
        return mode != null && mode.Param == null
            ? mode.Resolve(Array.Empty<object>())
            : MacroValues.DefaultFor(param.Type);
    }

    /// <summary>Wraps a nested routine so an exception inside it aborts the macro
    /// cleanly instead of killing the outer coroutine (which would leak the running flag).</summary>
    private static IEnumerator Guarded(IEnumerator inner, MacroRunToken token, Action<Exception> onError)
    {
        while (!token.Stopped)
        {
            object current;
            try
            {
                if (!inner.MoveNext()) yield break;
                current = inner.Current;
            }
            catch (Exception ex)
            {
                onError(ex);
                yield break;
            }
            yield return current;
        }
    }

    private static void Fail(Macro macro, MacroStep step, Exception ex)
    {
        var inner = ex is System.Reflection.TargetInvocationException tie && tie.InnerException != null
            ? tie.InnerException : ex;
        Plugin.LogSource.LogError($"[Macros] '{macro.Name}' step '{step.MethodId}' failed: {inner.Message}\n{inner.StackTrace}");
        StepFailed?.Invoke(macro, step, inner);
    }
}
