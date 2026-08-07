using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DynamicExpresso;
using lstwoMODS_Core.Hacks;

namespace lstwoMODS_Core.Macros;

/// <summary>
/// The engine behind <see cref="ExpressionValueSource"/>: a sandboxed DynamicExpresso
/// interpreter with a small math/utility function library. Expressions are C# syntax
/// (<c>current * 2</c>, <c>money > 100 ? 0 : current + 1</c>, string <c>+</c>, ...);
/// the only identifiers visible are the ones declared per call (<c>current</c>,
/// <c>prev</c>, named step outputs) plus the registered functions.
/// </summary>
public static class MacroExpressions
{
    private static readonly object _lock = new();
    private static readonly Random _random = new();
    private static readonly HashSet<string> _functionNames = new(StringComparer.Ordinal);
    private static Interpreter _interpreter;

    // Plugin registrations, remembered so they can be re-applied after an interpreter rebuild
    // (DynamicExpresso can't unbind a single identifier, so an unregister drops the interpreter
    // and the next GetInterpreter re-adds everything that's still registered).
    private static readonly Dictionary<string, Delegate> _customFunctions = new(StringComparer.Ordinal);
    private static readonly List<(Type Type, string Alias)> _customTypes = new();

    private const int MaxCachedLambdas = 256;
    private static readonly Dictionary<string, Lambda> _lambdaCache = new();

    /// <summary>Function list for editor help text.</summary>
    public const string FunctionHelp =
        "min max clamp abs sign floor ceil round roundto sqrt pow mod\n"
      + "map(v,inLo,inHi,outLo,outHi) snap(v,size) wrap(v,lo,hi) lerp(a,b,t)\n"
      + "sin cos tan atan2 deg rad · rand randint chance(p) pick(a,b,...)\n"
      + "approx(a,b,eps) str num · time dt unscaledTime realtime frame timescale fps\n"
      + "setting(\"Mod/Setting\") var(\"name\") var(\"name\",fallback) trigger(\"name\") trigger(\"name\",fallback)\n"
      + "vec(x,y,z)/vec3 vec2(x,y) dist dir vlerp rgb rgba hsv\n"
      + "Vector3/Color/Mathf statics; members work on typed values (pos.x, pos.magnitude)";

    /// <summary>Random choice among the arguments (a custom delegate so DynamicExpresso
    /// sees a real <c>params</c> parameter and expands call arguments into it).</summary>
    private delegate object PickDelegate(params object[] items);

    /// <summary><c>var("name")</c> or <c>var("name", fallback)</c>. The <c>params</c> tail lets
    /// the second argument be optional so old single-argument calls keep working; only the first
    /// fallback is used.</summary>
    private delegate object VarDelegate(string name, params object[] fallback);

    private static Interpreter GetInterpreter()
    {
        if (_interpreter != null) return _interpreter;

        var i = _interpreter = new Interpreter();
        Fn("min",     (Func<double, double, double>)Math.Min);
        Fn("max",     (Func<double, double, double>)Math.Max);
        Fn("abs",     (Func<double, double>)Math.Abs);
        Fn("sign",    (Func<double, double>)(v => Math.Sign(v)));
        Fn("floor",   (Func<double, double>)Math.Floor);
        Fn("ceil",    (Func<double, double>)Math.Ceiling);
        Fn("round",   (Func<double, double>)Math.Round);
        Fn("roundto", (Func<double, double, double>)((v, digits) => Math.Round(v, (int)digits)));
        Fn("sqrt",    (Func<double, double>)Math.Sqrt);
        Fn("pow",     (Func<double, double, double>)Math.Pow);
        Fn("clamp",   (Func<double, double, double, double>)((v, lo, hi) => Math.Min(Math.Max(v, lo), hi)));
        Fn("lerp",    (Func<double, double, double, double>)((a, b, t) => a + (b - a) * t));

        Fn("mod",  (Func<double, double, double>)((a, b) => ((a % b) + b) % b));
        Fn("map",  (Func<double, double, double, double, double, double>)((v, inLo, inHi, outLo, outHi)
            => outLo + (v - inLo) / (inHi - inLo) * (outHi - outLo)));
        Fn("snap", (Func<double, double, double>)((v, size) => Math.Round(v / size) * size));
        Fn("wrap", (Func<double, double, double, double>)((v, lo, hi) =>
        {
            var range = hi - lo;
            if (range <= 0) return lo;
            var r = (v - lo) % range;
            return (r < 0 ? r + range : r) + lo;
        }));

        Fn("sin",   (Func<double, double>)Math.Sin);
        Fn("cos",   (Func<double, double>)Math.Cos);
        Fn("tan",   (Func<double, double>)Math.Tan);
        Fn("atan2", (Func<double, double, double>)Math.Atan2);
        Fn("deg",   (Func<double, double>)(r => r * (180.0 / Math.PI)));
        Fn("rad",   (Func<double, double>)(d => d * (Math.PI / 180.0)));

        Fn("rand",    (Func<double, double, double>)((a, b) => { lock (_random) return a + _random.NextDouble() * (b - a); }));
        Fn("randint", (Func<double, double, int>)((a, b) => { lock (_random) return _random.Next((int)Math.Floor(a), (int)Math.Floor(b) + 1); }));
        Fn("chance",  (Func<double, bool>)(p => { lock (_random) return _random.NextDouble() < p; }));
        Fn("pick",    (PickDelegate)(items =>
        {
            if (items == null || items.Length == 0) throw new ArgumentException("pick() needs at least one argument.");
            lock (_random) return items[_random.Next(items.Length)];
        }));

        Fn("approx", (Func<double, double, double, bool>)((a, b, eps) => Math.Abs(a - b) <= eps));
        Fn("str",    (Func<object, string>)(v => Convert.ToString(v, CultureInfo.InvariantCulture) ?? ""));
        Fn("num",    (Func<object, double>)(v => Convert.ToDouble(v, CultureInfo.InvariantCulture)));

        Fn("time", (Func<double>)(() => UnityEngine.Time.time));
        Fn("dt",   (Func<double>)(() => UnityEngine.Time.deltaTime));
        Fn("unscaledTime", (Func<double>)(() => UnityEngine.Time.unscaledTime));
        Fn("realtime",     (Func<double>)(() => UnityEngine.Time.realtimeSinceStartup));
        Fn("frame",        (Func<int>)(() => UnityEngine.Time.frameCount));
        Fn("timescale",    (Func<double>)(() => UnityEngine.Time.timeScale));
        Fn("fps",          (Func<double>)(() => 1.0 / UnityEngine.Time.smoothDeltaTime));
        Fn("setting", (Func<string, object>)GetSettingValue);
        Fn("var", (VarDelegate)((name, fallback) =>
        {
            var v = MacroVariables.Resolve(name);
            if (v != null) return v;
            return fallback != null && fallback.Length > 0 ? fallback[0] : null;
        }));
        Fn("trigger", (VarDelegate)((name, fallback) =>
        {
            var v = MacroRunContext.Ambient?.GetTrigger(name);
            if (v != null) return v;
            return fallback != null && fallback.Length > 0 ? fallback[0] : null;
        }));

        var vec3 = (Func<object, object, object, UnityEngine.Vector3>)((x, y, z) => new UnityEngine.Vector3(F(x), F(y), F(z)));
        Fn("vec",   vec3);
        Fn("vec3",  vec3); // alias: the parameter label reads "vec3" (FriendlyTypeName), so the name should exist
        Fn("vec2",  (Func<object, object, UnityEngine.Vector2>)((x, y) => new UnityEngine.Vector2(F(x), F(y))));
        Fn("vec4",  (Func<object, object, object, object, UnityEngine.Vector4>)((x, y, z, w) => new UnityEngine.Vector4(F(x), F(y), F(z), F(w))));
        Fn("dist",  (Func<UnityEngine.Vector3, UnityEngine.Vector3, float>)UnityEngine.Vector3.Distance);
        Fn("dir",   (Func<UnityEngine.Vector3, UnityEngine.Vector3, UnityEngine.Vector3>)((from, to) => (to - from).normalized));
        Fn("vlerp", (Func<UnityEngine.Vector3, UnityEngine.Vector3, double, UnityEngine.Vector3>)((a, b, t) => UnityEngine.Vector3.Lerp(a, b, (float)t)));
        Fn("rgb",   (Func<object, object, object, UnityEngine.Color>)((r, g, b) => new UnityEngine.Color(F(r), F(g), F(b))));
        Fn("rgba",  (Func<object, object, object, object, UnityEngine.Color>)((r, g, b, a) => new UnityEngine.Color(F(r), F(g), F(b), F(a))));
        Fn("hsv",   (Func<object, object, object, UnityEngine.Color>)((h, s, v) => UnityEngine.Color.HSVToRGB(F(h), F(s), F(v))));

        i.Reference(typeof(UnityEngine.Vector2));
        i.Reference(typeof(UnityEngine.Vector3));
        i.Reference(typeof(UnityEngine.Quaternion));
        i.Reference(typeof(UnityEngine.Color));
        i.Reference(typeof(UnityEngine.Mathf));

        // The serializable flavours, for expressions written against a Vec2/Col parameter.
        // The constructors above still hand back Unity types; MacroValues.Coerce converts
        // between the two, so either spelling works wherever the other one did.
        i.Reference(typeof(Vec2));
        i.Reference(typeof(Vec3));
        i.Reference(typeof(Vec4));
        i.Reference(typeof(Col));

        // Plugin-registered functions and types, re-applied so they survive a rebuild.
        foreach (var kv in _customFunctions) Fn(kv.Key, kv.Value);
        foreach (var (type, alias) in _customTypes) ReferenceType(type, alias);

        return i;
    }

    /// <summary>Coerce any expression value (number, boxed float, numeric string) to a float for
    /// the vector/color constructors, so object-typed variables don't need a <c>num()</c> wrapper.</summary>
    private static float F(object v) => Convert.ToSingle(v, CultureInfo.InvariantCulture);

    /// <summary>The single registration path: keeps <see cref="_functionNames"/> in
    /// lockstep with what the interpreter actually knows.</summary>
    private static void Fn(string name, Delegate fn)
    {
        _interpreter.SetFunction(name, fn);
        _functionNames.Add(name);
    }

    /// <summary>
    /// Register an extra function usable in all macro expressions; plugins add
    /// game-specific vocabulary this way (<c>money()</c>, <c>isHost()</c>, ...).
    /// Re-registering a name replaces it.
    /// </summary>
    public static void RegisterFunction(string name, Delegate function)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentException("Function needs a name.");
        if (function == null) throw new ArgumentException("Function delegate is required.");
        lock (_lock)
        {
            GetInterpreter(); // built-ins first, so Fn targets a live interpreter
            _customFunctions[name] = function; // remembered so an interpreter rebuild re-applies it
            Fn(name, function);
            _lambdaCache.Clear(); // an expression that failed on the unknown name must re-parse
        }
    }

    /// <summary>Remove a function added by <see cref="RegisterFunction"/>. No-op for built-ins and
    /// unknown names. DynamicExpresso can't unbind an identifier, so the interpreter is rebuilt
    /// without it on next use.</summary>
    public static void UnregisterFunction(string name)
    {
        if (string.IsNullOrEmpty(name)) return;
        lock (_lock)
        {
            if (!_customFunctions.Remove(name)) return; // never drop a built-in
            _functionNames.Remove(name);
            _interpreter = null;   // force a clean rebuild without the removed function
            _lambdaCache.Clear();
        }
    }

    /// <summary>
    /// Make <paramref name="type"/> usable by name in expression text: its static members
    /// (<c>GameState.Money</c>), constructors (<c>new Color(...)</c>) and the type name itself
    /// become available, exactly like the built-in Vector3/Color/Mathf references. Pass
    /// <paramref name="alias"/> to expose it under a shorter name. Re-registering a type replaces
    /// its alias.
    /// </summary>
    public static void RegisterType(Type type, string alias = null)
    {
        if (type == null) throw new ArgumentException("Type is required.");
        lock (_lock)
        {
            GetInterpreter(); // built-ins first, so ReferenceType targets a live interpreter
            _customTypes.RemoveAll(t => t.Type == type);
            _customTypes.Add((type, alias));
            ReferenceType(type, alias);
            _lambdaCache.Clear();
        }
    }

    /// <summary>Remove a type reference added by <see cref="RegisterType"/>. No-op for built-ins and
    /// unknown types. Rebuilds the interpreter without it on next use.</summary>
    public static void UnregisterType(Type type)
    {
        if (type == null) return;
        lock (_lock)
        {
            if (_customTypes.RemoveAll(t => t.Type == type) == 0) return;
            _interpreter = null;
            _lambdaCache.Clear();
        }
    }

    private static void ReferenceType(Type type, string alias)
    {
        if (string.IsNullOrEmpty(alias)) _interpreter.Reference(type);
        else _interpreter.Reference(type, alias);
    }

    /// <summary>True when <paramref name="name"/> is a registered expression function
    /// (case-sensitive, like DynamicExpresso identifiers). Exists so editors can keep
    /// step output names from shadowing functions.</summary>
    public static bool IsFunctionName(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        lock (_lock)
        {
            GetInterpreter(); // built-ins register lazily
            return _functionNames.Contains(name);
        }
    }

    /// <summary>Resolve <c>setting("Mod/Setting")</c> (or a bare setting label when unique)
    /// to the live value of the [ModSetting], read from the same default-context instance
    /// as the projected Get step, so both agree inside a macro.</summary>
    private static object GetSettingValue(string name)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentException("setting() needs a name.");

        var matches = ModRegistry.AllSettings
            .Where(s => string.Equals($"{s.ModName}/{s.Label}", name, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (matches.Count == 0)
            matches = ModRegistry.AllSettings
                .Where(s => string.Equals(s.Label, name, StringComparison.OrdinalIgnoreCase))
                .ToList();

        if (matches.Count == 0)
            throw new ArgumentException($"setting(\"{name}\"): no such mod setting.");
        if (matches.Count > 1)
            throw new ArgumentException(
                $"setting(\"{name}\") is ambiguous, use \"Mod/Setting\": "
                + string.Join(", ", matches.Take(4).Select(s => $"{s.ModName}/{s.Label}")));

        var match = matches[0];
        return MacroRegistry.GetDefaultContextValue(match, ModRegistry.GetContextRequirements(match.Mod.GetType()));
    }

    /// <summary>
    /// Evaluate <paramref name="text"/> with the given variables in scope. Parsed lambdas are
    /// cached by (text, variable signature), so repeated runs of the same step only pay for an
    /// Invoke. Throws (with DynamicExpresso's message) on parse or evaluation errors; the
    /// macro runner reports that as a step failure.
    /// </summary>
    public static object Evaluate(string text, IReadOnlyList<KeyValuePair<string, object>> variables)
    {
        var types = variables.Select(v => new KeyValuePair<string, Type>(v.Key, v.Value?.GetType() ?? typeof(object))).ToList();
        var lambda = GetLambda(text, types);
        return lambda.Invoke(variables.Select(v => v.Value).ToArray());
    }

    /// <summary>
    /// Edit-time validation: parse (type-check included) against the declared variables.
    /// Returns null when the expression is valid, otherwise a human-readable error.
    /// </summary>
    public static string Validate(string text, IReadOnlyList<KeyValuePair<string, Type>> variables)
    {
        if (string.IsNullOrWhiteSpace(text)) return "empty expression";
        try
        {
            GetLambda(text, variables);
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    private static Lambda GetLambda(string text, IReadOnlyList<KeyValuePair<string, Type>> variables)
    {
        var key = text + "\n" + string.Join("|", variables.Select(v => $"{v.Key}:{v.Value.FullName}"));
        lock (_lock)
        {
            if (_lambdaCache.TryGetValue(key, out var cached)) return cached;

            var parameters = variables.Select(v => new Parameter(v.Key, v.Value)).ToArray();
            var lambda = GetInterpreter().Parse(text, parameters);

            if (_lambdaCache.Count >= MaxCachedLambdas) _lambdaCache.Clear();
            _lambdaCache[key] = lambda;
            return lambda;
        }
    }
}
