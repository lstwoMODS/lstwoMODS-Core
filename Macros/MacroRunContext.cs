using System;
using System.Collections.Generic;
using System.Linq;

namespace lstwoMODS_Core.Macros;

/// <summary>
/// Per-run state handed to <see cref="ValueSource.Evaluate"/>: the outputs of every step
/// that has executed so far in this run. Steps run strictly in order and the first failure
/// aborts the run, so a later step can rely on every earlier output being present.
/// </summary>
public class MacroRunContext
{
    /// <summary>Step output by <see cref="MacroStep.Id"/> (reorder-proof references).</summary>
    public readonly Dictionary<string, object> OutputsByStepId = new();

    /// <summary>Step output by <see cref="MacroStep.OutputName"/> (expression references).
    /// If two steps share a name, the most recently executed one wins.</summary>
    public readonly Dictionary<string, object> OutputsByName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Output of the immediately preceding step (<c>prev</c> in expressions).</summary>
    public object LastOutput;

    /// <summary>The macro this run is executing. Set by <see cref="MacroRunner"/>.</summary>
    public Macro Macro;

    /// <summary>Set by the Return step (<c>core.return</c>) to stop the remaining steps of this
    /// run. The runner checks it after each step.</summary>
    public bool Returned;

    /// <summary>Value handed back by the Return step, surfaced as the waited caller's step output
    /// (a <c>Run Macro</c> / If / Switch that waited on this macro). Null when the macro fell off
    /// the end without returning a value.</summary>
    public object ReturnValue;

    /// <summary>Empty stand-in for a run started without a trigger (editor Play, Run Macro).</summary>
    internal static readonly IReadOnlyDictionary<string, object> NoTrigger =
        new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    /// <summary>The named values the trigger fired this run with (which player, how much money, ...),
    /// keyed by the trigger's declared <c>MacroTriggerOutput.Key</c>. Empty when no trigger supplied
    /// values. Read in expressions as bare variables or via <c>trigger("name")</c>; see
    /// <see cref="TriggerVariables"/>.</summary>
    public IReadOnlyDictionary<string, object> TriggerValues = NoTrigger;

    /// <summary>The run context of the step currently executing, set by the runner across argument
    /// evaluation and Execute (single-threaded coroutine host, like <see cref="MacroVariables.Current"/>).
    /// Lets the static <c>trigger()</c> expression function reach this run's <see cref="TriggerValues"/>
    /// the way <c>var()</c> reaches the ambient variable scope. Null outside a run.</summary>
    public static MacroRunContext Ambient;

    /// <summary>This run's value for a trigger output, or null when the trigger didn't supply it (or
    /// no trigger fired). What <c>trigger("name")</c> resolves to before applying its fallback.</summary>
    public object GetTrigger(string name)
        => !string.IsNullOrEmpty(name) && TriggerValues != null && TriggerValues.TryGetValue(name, out var v) ? v : null;

    /// <summary>
    /// The trigger's outputs as expression variables: one per declared <c>MacroTriggerOutput</c> of
    /// this macro's trigger, bound to the fired value when present and the output's type default
    /// otherwise. Declaring drives both edit-time validation (so a bare <c>player</c> type-checks)
    /// and this runtime binding, so referencing a trigger output never throws even on a manual run.
    /// Empty when the trigger declares none.
    /// </summary>
    public IEnumerable<KeyValuePair<string, object>> TriggerVariables()
    {
        var outputs = MacroTriggerRegistry.For(Macro?.Trigger)?.ResolveOutputs(Macro?.Trigger);
        if (outputs == null) yield break;
        foreach (var output in outputs)
        {
            if (string.IsNullOrEmpty(output?.Key)) continue;
            var value = TriggerValues != null && TriggerValues.TryGetValue(output.Key, out var v)
                ? v
                : MacroValues.DefaultFor(output.Type);
            yield return new KeyValuePair<string, object>(output.Key, value);
        }
    }

    public void RecordOutput(MacroStep step, object output)
    {
        OutputsByStepId[step.Id] = output;
        if (!string.IsNullOrEmpty(step.OutputName))
            OutputsByName[step.OutputName] = output;
        LastOutput = output;
    }

    /// <summary>
    /// Evaluate a C# expression string against this run's live variables (<c>prev</c>, every named
    /// step output, plus the <c>var()</c> function and the rest of the library)  the same set an
    /// <see cref="ExpressionValueSource"/> sees, minus the parameter-bound <c>current</c>. Used by
    /// custom steps (Switch) so their expressions behave exactly like ordinary Expr-mode arguments.
    /// </summary>
    public object EvaluateExpression(string text)
    {
        var variables = new List<KeyValuePair<string, object>>
        {
            new("prev", LastOutput),
        };
        void Add(string name, object value)
        {
            if (variables.All(v => !string.Equals(v.Key, name, StringComparison.OrdinalIgnoreCase)))
                variables.Add(new KeyValuePair<string, object>(name, value));
        }
        foreach (var kv in OutputsByName) Add(kv.Key, kv.Value);
        // Trigger outputs are lowest precedence: a step output of the same name shadows them.
        foreach (var kv in TriggerVariables()) Add(kv.Key, kv.Value);
        return MacroExpressions.Evaluate(text, variables);
    }
}
