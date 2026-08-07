using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using lstwoMODS_Core.Hacks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace lstwoMODS_Core.Macros;

/// <summary>One parameter of a macro-callable method.</summary>
public class MacroParam
{
    public string Name;
    public Type Type;

    /// <summary>True for parameters injected from the mod's [ModContext] requirements
    /// (e.g. the Player a projected setting applies to) rather than declared by the
    /// method itself. The editor shows these above the step's other options.</summary>
    public bool IsContext;

    /// <summary>New steps default this parameter to Expr mode instead of a constant
    /// (used by the Expression builtin, whose whole point is the expression).</summary>
    public bool PrefersExpression;

    /// <summary>Reads the current value of whatever this parameter feeds, or null when
    /// unavailable. Setting-projected "Set X" methods provide it automatically; it is what
    /// makes the Toggle value source possible for bools.</summary>
    public Func<object> CurrentValueGetter;
}

/// <summary>
/// A method macros can call. Everything a step can do (mod actions, setting get/set,
/// waits, plugin helpers) is one of these; the engine has no other step kinds.
/// </summary>
public class MacroMethodDescriptor
{
    /// <summary>Stable id persisted in macro JSON: <c>{DeclaringType.FullName}.{Member}</c>
    /// for registry projections (settings add <c>.get</c>/<c>.set</c>), explicit ids like
    /// <c>"core.wait"</c> for custom registrations.</summary>
    public string Id;

    public string Label;

    /// <summary>Picker grouping as a '/'-separated path: every segment becomes a nested
    /// group in the add-step picker (e.g. "Flow", "Mods/FrogMods", "Mods/FrogMods/Walk Speed").
    /// The ModRegistry projection emits "Mods/{mod}" for actions and "Mods/{mod}/{setting}"
    /// for setting get/set pairs.</summary>
    public string Category;

    /// <summary>Optional short label for the picker tree leaf (e.g. "Get" under a setting
    /// group). Falls back to <see cref="Label"/>, which step headers always use.</summary>
    public string PickerLabel;

    public MacroParam[] Parameters = Array.Empty<MacroParam>();

    public Type ReturnType = typeof(void);

    /// <summary>The type of the value this step produces as a named output, when that differs from
    /// <see cref="ReturnType"/>. Set this for a step that returns an <see cref="IEnumerator"/> to
    /// the runner (so it gets yielded) yet still yields a usable value once it completes: a waited
    /// macro call (Run Macro / If / Switch) returns the callee's Return value this way, and a
    /// plugin step returns its own by handing back a <see cref="MacroValueRoutine"/>. This only
    /// declares the type for the editor; a bare <see cref="IEnumerator"/> still records no output
    /// however this is set. Null = derive from <see cref="ReturnType"/> (the normal case).</summary>
    public Type OutputType;

    /// <summary>Invoked with one evaluated argument per <see cref="Parameters"/> entry.
    /// May return an <see cref="IEnumerator"/>, which the runner yields inside its
    /// coroutine (how <c>core.wait</c> works); any other value is the step's output.
    /// Ignored when <see cref="ExecuteCustom"/> is set.</summary>
    public Func<object[], object> Execute;

    /// <summary>Optional custom editor for steps that don't fit the one-row-per-parameter layout
    /// (a variable number of sub-widgets, etc.). When set, the editor renders this instead of
    /// building parameter rows, and the step stores its data in <see cref="MacroStep.Custom"/>.
    /// Pair with <see cref="ExecuteCustom"/>. See <see cref="MacroSwitchStep"/> for a worked
    /// example built entirely on this public API.</summary>
    public MacroStepEditor CustomEditor;

    /// <summary>Run path for a custom step: called with the step and run context instead of the
    /// per-parameter <see cref="Execute"/>. Return value is treated identically (an
    /// <see cref="IEnumerator"/> is yielded; anything else is the step output). Used when the step
    /// reads <see cref="MacroStep.Custom"/> rather than evaluated parameters.</summary>
    public Func<CustomStepRunContext, object> ExecuteCustom;
}

/// <summary>
/// What a custom step's <see cref="MacroMethodDescriptor.ExecuteCustom"/> receives: the step (for
/// its <see cref="MacroStep.Custom"/> data), the current <see cref="MacroRunContext"/> and
/// <see cref="MacroCallChain"/>, plus helpers to evaluate expressions and value sources the same
/// way ordinary step arguments are evaluated.
/// </summary>
public sealed class CustomStepRunContext
{
    public MacroStep Step { get; }
    public MacroRunContext Context { get; }
    public MacroCallChain Chain { get; }

    public CustomStepRunContext(MacroStep step, MacroRunContext context, MacroCallChain chain)
    {
        Step = step;
        Context = context;
        Chain = chain;
    }

    /// <summary>Evaluate a C# expression string against the run's live variables (same as an
    /// Expr-mode argument, minus <c>current</c>).</summary>
    public object Eval(string expression) => Context.EvaluateExpression(expression);

    /// <summary>Evaluate a stored <see cref="ValueSource"/> for a parameter of the given type.</summary>
    public object Eval(ValueSource source, Type type)
        => source?.Evaluate(new MacroParam { Name = "value", Type = type }, Context);
}

/// <summary>
/// Database of all macro-callable methods: explicit registrations (built-ins, plugin
/// helpers) plus a live projection of every [ModAction] and [ModSetting] in
/// <see cref="ModRegistry"/>: actions 1:1, settings as get/set pairs.
/// </summary>
public static class MacroRegistry
{
    private static readonly List<MacroMethodDescriptor> _methods = new();

    // Cached projection: Find() runs once per step per macro run, so rebuilding the
    // descriptors every lookup would churn allocations every frame in Wait-driven loops.
    // Rebuilt lazily when ModRegistry mutates or Register() is called.
    private static List<MacroMethodDescriptor> _cache;
    private static Dictionary<string, MacroMethodDescriptor> _byId;
    private static int _cacheVersion = -1;

    static MacroRegistry()
    {
        RegisterBuiltins();
    }

    public static void Register(MacroMethodDescriptor descriptor)
    {
        if (string.IsNullOrEmpty(descriptor?.Id)) throw new ArgumentException("Descriptor needs a non-empty Id.");
        _methods.RemoveAll(m => m.Id == descriptor.Id);
        _methods.Add(descriptor);
        _cache = null;
    }

    /// <summary>Register from a delegate; parameters and return type come from reflection.</summary>
    public static void Register(string id, string label, Delegate method, string category = "Misc")
    {
        var mi = method.Method;
        Register(new MacroMethodDescriptor
        {
            Id = id,
            Label = label,
            Category = category,
            ReturnType = mi.ReturnType,
            Parameters = mi.GetParameters()
                .Select(p => new MacroParam { Name = p.Name, Type = p.ParameterType }).ToArray(),
            Execute = args => method.DynamicInvoke(args),
        });
    }

    /// <summary>All callable methods, including the ModRegistry projection. Returns a copy;
    /// the projection itself is cached until the registry changes.</summary>
    public static List<MacroMethodDescriptor> GetAll()
        => new(BuildCache());

    /// <summary>Resolve a persisted method id, or null (mod uninstalled, id renamed).</summary>
    public static MacroMethodDescriptor Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        BuildCache();
        return _byId.TryGetValue(id, out var m) ? m : null;
    }

    private static List<MacroMethodDescriptor> BuildCache()
    {
        if (_cache != null && _cacheVersion == ModRegistry.Version) return _cache;

        var list = new List<MacroMethodDescriptor>(_methods);
        list.AddRange(ProjectModRegistry());

        // First registration wins on id collisions (overloads, duplicate mod instances),
        // matching the old FirstOrDefault semantics; warn once per rebuild.
        var byId = new Dictionary<string, MacroMethodDescriptor>(list.Count);
        List<string> dupes = null;
        foreach (var m in list)
        {
            if (!byId.ContainsKey(m.Id)) byId.Add(m.Id, m);
            else if (dupes == null || !dupes.Contains(m.Id)) (dupes ??= new()).Add(m.Id);
        }
        if (dupes != null)
            Plugin.LogSource.LogWarning(
                "[Macros] Duplicate method ids, steps will bind to the first one: " + string.Join(", ", dupes));

        _byId = byId;
        _cacheVersion = ModRegistry.Version;
        return _cache = list;
    }

    private static IEnumerable<MacroMethodDescriptor> ProjectModRegistry()
    {
        foreach (var action in ModRegistry.AllActions)
        {
            var a = action;
            var reqs = ModRegistry.GetContextRequirements(a.Mod.GetType());

            yield return new MacroMethodDescriptor
            {
                Id = $"{a.InvokeTarget.GetType().FullName}.{a.MethodName}",
                Label = a.Label,
                Category = $"Mods/{a.ModName}",
                ReturnType = a.ReturnType,
                Parameters = ContextParams(reqs)
                    .Concat(a.Parameters.Where(p => !p.IsExcluded)
                        .Select(p => new MacroParam { Name = p.Label, Type = p.ParameterType }))
                    .ToArray(),
                Execute = args =>
                {
                    var target = ResolveDetachedTarget(a.Mod, reqs, args, out var rest);
                    return a.Invoke(target, rest);
                },
            };
        }

        foreach (var setting in ModRegistry.AllSettings)
        {
            var s = setting;
            var reqs = ModRegistry.GetContextRequirements(s.Mod.GetType());
            var baseId = $"{s.ValueTarget.GetType().FullName}.{s.MemberName}";

            yield return new MacroMethodDescriptor
            {
                Id = baseId + ".get",
                Label = $"Get {s.Label}",
                PickerLabel = "Get",
                Category = $"Mods/{s.ModName}/{s.Label}",
                ReturnType = s.ValueType,
                Parameters = ContextParams(reqs),
                Execute = args =>
                {
                    var target = ResolveDetachedTarget(s.Mod, reqs, args, out _);
                    return s.GetValue(target);
                },
            };

            yield return new MacroMethodDescriptor
            {
                Id = baseId + ".set",
                Label = $"Set {s.Label}",
                PickerLabel = "Set",
                Category = $"Mods/{s.ModName}/{s.Label}",
                Parameters = ContextParams(reqs)
                    .Concat(new[]
                    {
                        new MacroParam { Name = "value", Type = s.ValueType, CurrentValueGetter = () => GetDefaultContextValue(s, reqs) },
                    })
                    .ToArray(),
                Execute = args =>
                {
                    var target = ResolveDetachedTarget(s.Mod, reqs, args, out var rest);
                    s.SetValue(target, rest[0]);
                    return null;
                },
            };
        }
    }

    private static MacroParam[] ContextParams(IReadOnlyList<ContextRequirement> reqs)
        => reqs.Select(r => new MacroParam { Name = r.Key, Type = r.ContextType, IsContext = true }).ToArray();

    /// <summary>
    /// Resolve the detached mod instance for a projected invocation: the leading
    /// <paramref name="args"/> (one per context requirement) select/create the per-context
    /// instance and are injected as its <see cref="ModExecutionContext"/>; the rest are the
    /// method's own arguments. Mods invoked through macros never touch the UI instance.
    /// </summary>
    private static BaseMod ResolveDetachedTarget(BaseMod uiMod, IReadOnlyList<ContextRequirement> reqs,
        object[] args, out object[] rest)
    {
        var modType = uiMod.GetType();
        rest = reqs.Count > 0 ? args.Skip(reqs.Count).ToArray() : args;

        object cacheKey = null;
        var ctx = new ModExecutionContext();
        for (var i = 0; i < reqs.Count; i++)
        {
            var value = args[i];
            ctx.With(reqs[i].Key, value);
            var key = MacroTypes.CacheKeyFor(reqs[i].ContextType, value);
            cacheKey = cacheKey == null ? key : (cacheKey, key);
        }

        var instance = ModRegistry.GetDetachedInstance(modType, cacheKey);
        instance.SetContext(ctx);
        return instance;
    }

    /// <summary>
    /// Live value for Toggle / <c>current</c> on a projected setting: read from the detached
    /// instance for the type's default context (e.g. the local player). Falls back to the
    /// registered UI instance when the context can't be resolved (no game yet, no registered
    /// macro type, or the default mode needs an argument). Also used by
    /// <see cref="MacroExpressions"/> so <c>setting()</c> reads the same instance as Get steps.
    /// </summary>
    internal static object GetDefaultContextValue(ModSettingDescriptor s, IReadOnlyList<ContextRequirement> reqs)
    {
        try
        {
            var args = new object[reqs.Count];
            for (var i = 0; i < reqs.Count; i++)
            {
                var mode = MacroTypes.For(reqs[i].ContextType)?.DefaultMode;
                if (mode == null || mode.Param != null)
                    return s.GetValue(); // no parameterless default, use the UI instance
                args[i] = mode.Resolve(Array.Empty<object>());
            }

            var target = ResolveDetachedTarget(s.Mod, reqs, args, out _);
            return s.GetValue(target);
        }
        catch
        {
            return s.GetValue();
        }
    }

    private static void RegisterBuiltins()
    {
        // Macros themselves are a selectable object type: Run Macro's "macro" parameter
        // gets a dropdown of call ids, and expression/step-output strings resolve by
        // id / slug / unique name; duplicates never break because slugs are unique.
        MacroTypes.Register(new MacroTypeDescriptor
        {
            Type = typeof(Macro),
            DisplayName = "Macro",
            DefaultModeId = "pick",
            ResolveFromString = MacroManager.FindByRef,
            Modes =
            {
                new MacroTypeMode
                {
                    Id = "pick", Label = "Pick",
                    Param = new MacroParam { Name = "macro", Type = typeof(string) },
                    Resolve = args => MacroManager.FindByRef((string)args[0]),
                    Choices = () => MacroManager.Macros.Select(m => m.Slug).ToArray(),
                },
            },
        });

        Register(new MacroMethodDescriptor
        {
            Id = "core.wait",
            Label = "Wait",
            Category = "Flow",
            Parameters = new[] { new MacroParam { Name = "seconds", Type = typeof(float) } },
            ReturnType = typeof(IEnumerator),
            Execute = args => MacroFlow.Wait((float)MacroValues.Coerce(args[0], typeof(float))),
        });

        // Run Macro is a custom-editor step (pick or expression target, optional arguments); see
        // MacroRunStep, registered at the end of this method alongside the other custom steps.

        Register(new MacroMethodDescriptor
        {
            Id = "core.expr",
            Label = "Expression",
            Category = "Flow",
            Parameters = new[] { new MacroParam { Name = "value", Type = typeof(object), PrefersExpression = true } },
            ReturnType = typeof(object),
            Execute = args => args[0],
        });

        // Log: print a message to the BepInEx console for debugging macros. The message defaults to
        // Expr mode so you can log variables/step outputs directly ("health = " + var("health")).
        Register(new MacroMethodDescriptor
        {
            Id = "core.log",
            Label = "Log",
            Category = "Flow",
            Parameters = new[]
            {
                new MacroParam { Name = "message", Type = typeof(object), PrefersExpression = true },
                new MacroParam { Name = "level", Type = typeof(MacroLogLevel) },
            },
            ReturnType = typeof(void),
            Execute = args =>
            {
                var level = (MacroLogLevel)MacroValues.Coerce(args[1], typeof(MacroLogLevel));
                MacroLog.Write(level, "[Macro] " + MacroValues.Coerce(args[0], typeof(string)));
                return null;
            },
        });

        // ── Conditionals ──────────────────────────────────────────────────
        // If: run one of two macros depending on a condition. Like Run Macro, an empty macro
        // reference means "run nothing", so an if with no else just skips when false.
        Register(new MacroMethodDescriptor
        {
            Id = "core.if",
            Label = "If",
            Category = "Flow",
            Parameters = new[]
            {
                new MacroParam { Name = "condition", Type = typeof(bool), PrefersExpression = true },
                new MacroParam { Name = "then", Type = typeof(Macro) },
                new MacroParam { Name = "else", Type = typeof(Macro) },
                new MacroParam { Name = "wait", Type = typeof(bool) },
            },
            ReturnType = typeof(IEnumerator),
            OutputType = typeof(object), // a waited call surfaces the chosen macro's Return value
            Execute = args =>
            {
                var cond = (bool)MacroValues.Coerce(args[0], typeof(bool));
                var target = (cond ? args[1] : args[2]) as Macro;
                return RunPicked(target, (bool)MacroValues.Coerce(args[3], typeof(bool)));
            },
        });

        // Return: stop the remaining steps of the current macro, optionally handing a value back
        // to a waited caller. "when" gates it so a bare Return step can early-exit conditionally
        // in the flat step list ("return when health <= 0").
        Register(new MacroMethodDescriptor
        {
            Id = "core.return",
            Label = "Return",
            Category = "Flow",
            Parameters = new[]
            {
                // Default constant true (a bare Return exits now); switch to Expr for a condition.
                new MacroParam { Name = "when", Type = typeof(bool), CurrentValueGetter = () => true },
                // Optional; default empty constant so a value-less early-exit doesn't error. Switch
                // to Expr (or type a literal) to hand a value back to a waited caller.
                new MacroParam { Name = "value", Type = typeof(object) },
            },
            ReturnType = typeof(void),
            Execute = args =>
            {
                if (!(bool)MacroValues.Coerce(args[0], typeof(bool))) return null;
                var ctx = MacroRunner.CurrentContext;
                if (ctx != null)
                {
                    ctx.Returned = true;
                    ctx.ReturnValue = args[1];
                }
                return null;
            },
        });

        // ── Variables ─────────────────────────────────────────────────────
        Register(new MacroMethodDescriptor
        {
            Id = "core.setVar",
            Label = "Set Variable",
            Category = "Flow/Variables",
            PickerLabel = "Set Variable",
            Parameters = new[]
            {
                new MacroParam { Name = "scope", Type = typeof(MacroVarScope) },
                new MacroParam { Name = "name", Type = typeof(string) },
                new MacroParam { Name = "value", Type = typeof(object), PrefersExpression = true },
            },
            ReturnType = typeof(void),
            Execute = args =>
            {
                var scope = (MacroVarScope)MacroValues.Coerce(args[0], typeof(MacroVarScope));
                MacroVariables.Set(scope, (string)MacroValues.Coerce(args[1], typeof(string)), args[2]);
                return null;
            },
        });

        Register(new MacroMethodDescriptor
        {
            Id = "core.getVar",
            Label = "Get Variable",
            Category = "Flow/Variables",
            PickerLabel = "Get Variable",
            Parameters = new[]
            {
                new MacroParam { Name = "scope", Type = typeof(MacroVarScope) },
                new MacroParam { Name = "name", Type = typeof(string) },
                new MacroParam { Name = "fallback", Type = typeof(object), PrefersExpression = true },
            },
            ReturnType = typeof(object),
            Execute = args =>
            {
                var scope = (MacroVarScope)MacroValues.Coerce(args[0], typeof(MacroVarScope));
                // Fall back to the third arg when the variable is unset or null; old two-arg
                // macros leave it null, which reproduces the original "return null" behaviour.
                return MacroVariables.Get(scope, (string)MacroValues.Coerce(args[1], typeof(string)))
                    ?? args[2];
            },
        });

        // Save/Load: the durable counterpart to Set/Get  a value written under a key survives
        // restarts (stored as JSON via DataStorage). Load returns the stored token, coerced to the
        // consuming parameter's type like any other value.
        Register(new MacroMethodDescriptor
        {
            Id = "core.saveVar",
            Label = "Save Variable",
            Category = "Flow/Variables",
            PickerLabel = "Save Variable (to disk)",
            Parameters = new[]
            {
                new MacroParam { Name = "key", Type = typeof(string) },
                new MacroParam { Name = "value", Type = typeof(object), PrefersExpression = true },
            },
            ReturnType = typeof(void),
            Execute = args =>
            {
                var key = SanitizeVarKey((string)MacroValues.Coerce(args[0], typeof(string)));
                if (key.Length > 0) DataStorage.Save("lstwoMODS_Core", "macros/vars/" + key, args[1]);
                return null;
            },
        });

        Register(new MacroMethodDescriptor
        {
            Id = "core.loadVar",
            Label = "Load Variable",
            Category = "Flow/Variables",
            PickerLabel = "Load Variable (from disk)",
            Parameters = new[] { new MacroParam { Name = "key", Type = typeof(string) } },
            ReturnType = typeof(object),
            Execute = args =>
            {
                var key = SanitizeVarKey((string)MacroValues.Coerce(args[0], typeof(string)));
                return key.Length == 0 ? null : DataStorage.Load<JToken>("lstwoMODS_Core", "macros/vars/" + key);
            },
        });

        // ── Loops ─────────────────────────────────────────────────────────
        // Repeat: run a macro a fixed number of times. Each run gets index (1-based) and count as
        // trigger values, so the body reads them as trigger("index")/trigger("count") (or bare
        // variables when it declares them via a Called-by-Macro trigger).
        Register(new MacroMethodDescriptor
        {
            Id = "core.repeat",
            Label = "Repeat",
            Category = "Flow",
            PickerLabel = "Repeat (N times)",
            Parameters = new[]
            {
                new MacroParam { Name = "times", Type = typeof(int), PrefersExpression = true },
                new MacroParam { Name = "macro", Type = typeof(Macro) },
                new MacroParam { Name = "wait", Type = typeof(bool), CurrentValueGetter = () => true },
            },
            ReturnType = typeof(IEnumerator),
            Execute = args =>
            {
                var times = (int)MacroValues.Coerce(args[0], typeof(int));
                var target = args[1] as Macro;
                var wait = (bool)MacroValues.Coerce(args[2], typeof(bool));
                return RepeatRoutine(times, target, wait, MacroRunner.CurrentChain);
            },
        });

        // For Each: iterate a collection (an array/list/enumerable from a step output or an
        // expression) and run a macro per element, handing it item + index (1-based) as trigger
        // values. A non-enumerable value runs the body once with that single item.
        Register(new MacroMethodDescriptor
        {
            Id = "core.forEach",
            Label = "For Each",
            Category = "Flow",
            PickerLabel = "For Each (item in list)",
            Parameters = new[]
            {
                new MacroParam { Name = "items", Type = typeof(object), PrefersExpression = true },
                new MacroParam { Name = "macro", Type = typeof(Macro) },
                new MacroParam { Name = "wait", Type = typeof(bool), CurrentValueGetter = () => true },
            },
            ReturnType = typeof(IEnumerator),
            Execute = args =>
            {
                var target = args[1] as Macro;
                var wait = (bool)MacroValues.Coerce(args[2], typeof(bool));
                return ForEachRoutine(args[0], target, wait, MacroRunner.CurrentChain);
            },
        });

        MacroRunStep.Register();
        MacroSwitchStep.Register();
    }

    private static readonly StringComparer LoopKeyCmp = StringComparer.OrdinalIgnoreCase;

    private static IEnumerator RepeatRoutine(int times, Macro target, bool wait, MacroCallChain chain)
    {
        if (target == null || times <= 0) yield break;
        for (var i = 1; i <= times; i++)
        {
            if (chain != null && chain.Stopped) yield break;
            var values = new Dictionary<string, object>(LoopKeyCmp) { ["index"] = i, ["count"] = times };
            if (wait) yield return MacroRunner.RunNested(target, chain, false, values);
            else MacroRunner.RunDetached(target, chain, false, values);
        }
    }

    private static IEnumerator ForEachRoutine(object items, Macro target, bool wait, MacroCallChain chain)
    {
        if (target == null || items == null) yield break;

        var i = 0;
        foreach (var item in AsEnumerable(items))
        {
            if (chain != null && chain.Stopped) yield break;
            i++;
            var values = new Dictionary<string, object>(LoopKeyCmp) { ["item"] = item, ["index"] = i };
            if (wait) yield return MacroRunner.RunNested(target, chain, false, values);
            else MacroRunner.RunDetached(target, chain, false, values);
        }
    }

    /// <summary>Flatten a value for For Each: an enumerable yields its elements; a string is a
    /// single item (not its characters); anything else is one item.</summary>
    private static IEnumerable AsEnumerable(object items)
    {
        if (items is string) return new[] { items };
        if (items is IEnumerable e) return e.Cast<object>();
        return new[] { items };
    }

    /// <summary>Shared macro-dispatch for the If and Switch steps: run <paramref name="target"/>
    /// (null = nothing) as a waited sub-call (returns its value) or a detached tail call. Mirrors
    /// <c>core.runMacro</c>, including Toggle on/off alternation.</summary>
    internal static IEnumerator RunPicked(Macro target, bool wait)
    {
        if (target == null) return null;
        var chain = MacroRunner.CurrentChain;
        var off = MacroManager.AdvanceToggle(target);
        if (wait) return MacroRunner.RunNested(target, chain, off);
        MacroRunner.RunDetached(target, chain, off);
        return null;
    }

    /// <summary>Filesystem-safe stem for a save/load key: keeps identifier characters, dots and
    /// dashes, drops path separators. Empty means "no key", which the steps skip.</summary>
    private static string SanitizeVarKey(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        var sb = new System.Text.StringBuilder(raw.Length);
        foreach (var ch in raw.Trim())
            if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' || ch == '.')
                sb.Append(ch);
        return sb.ToString();
    }

}
