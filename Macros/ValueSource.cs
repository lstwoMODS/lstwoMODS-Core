using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace lstwoMODS_Core.Macros;

/// <summary>
/// How a single step argument gets its value at run time. Serialized as an object
/// with a <c>"kind"</c> discriminator; new kinds (step outputs, expressions) register
/// via <see cref="ValueSourceConverter.RegisterKind"/>.
/// </summary>
[JsonConverter(typeof(ValueSourceConverter))]
public abstract class ValueSource
{
    /// <param name="ctx">Outputs of the steps already executed this run. Null when
    /// evaluated outside a run (editor previews).</param>
    public abstract object Evaluate(MacroParam param, MacroRunContext ctx);
}

/// <summary>A typed literal, coerced to the parameter type when the step runs.</summary>
public class ConstantValueSource : ValueSource
{
    /// <summary>Raw literal: a string as edited in the UI, or a JSON primitive as
    /// loaded from disk. <see cref="MacroValues.Coerce"/> normalizes either form.</summary>
    public object Value;

    public override object Evaluate(MacroParam param, MacroRunContext ctx) => MacroValues.Coerce(Value, param.Type);
}

/// <summary>
/// Evaluates to the inverse of the parameter's current value. Only meaningful for
/// bool parameters whose method provides a <see cref="MacroParam.CurrentValueGetter"/>
/// (setting-projected "Set X" methods do automatically); "toggle noclip" is
/// <c>Set Enabled(value: Toggle)</c>.
/// </summary>
public class ToggleValueSource : ValueSource
{
    public override object Evaluate(MacroParam param, MacroRunContext ctx)
    {
        if (param.CurrentValueGetter == null)
            throw new InvalidOperationException($"Parameter '{param.Name}' does not support Toggle (no current-value getter).");
        return !(bool)MacroValues.Coerce(param.CurrentValueGetter(), typeof(bool));
    }
}

/// <summary>
/// The return value of an earlier step in the same run, referenced by step id
/// (reorder-proof; the editor shows a dropdown, never the raw id).
/// </summary>
public class StepOutputValueSource : ValueSource
{
    /// <summary><see cref="MacroStep.Id"/> of the producing step.</summary>
    public string StepId = "";

    public override object Evaluate(MacroParam param, MacroRunContext ctx)
    {
        if (ctx == null || string.IsNullOrEmpty(StepId) || !ctx.OutputsByStepId.TryGetValue(StepId, out var value))
            throw new InvalidOperationException(
                $"Parameter '{param.Name}' references a step output that is not available (step removed, or it runs later).");
        return MacroValues.Coerce(value, param.Type);
    }
}

/// <summary>
/// A C#-syntax expression evaluated by <see cref="MacroExpressions"/> when the step runs.
/// In scope: <c>current</c> (the parameter's live value, when the method provides a getter),
/// <c>prev</c> (previous step's output), every named step output executed so far, and the
/// function library. The result is coerced to the parameter type like any constant.
/// </summary>
public class ExpressionValueSource : ValueSource
{
    public string Text = "";

    public override object Evaluate(MacroParam param, MacroRunContext ctx)
    {
        var variables = new List<KeyValuePair<string, object>>();
        void Add(string name, object value)
        {
            // current/prev win over a colliding output name (matches editor validation)
            if (variables.All(v => !string.Equals(v.Key, name, StringComparison.OrdinalIgnoreCase)))
                variables.Add(new KeyValuePair<string, object>(name, value));
        }

        if (param.CurrentValueGetter != null)
            Add("current", param.CurrentValueGetter());
        if (ctx != null)
        {
            Add("prev", ctx.LastOutput);
            foreach (var kv in ctx.OutputsByName)
                Add(kv.Key, kv.Value);
            // Trigger outputs come last: current/prev and any named step output shadow them.
            foreach (var kv in ctx.TriggerVariables())
                Add(kv.Key, kv.Value);
        }

        var result = MacroExpressions.Evaluate(Text, variables);
        return MacroValues.Coerce(result, param.Type);
    }
}

/// <summary>
/// A value chosen through one of a registered <see cref="MacroTypeDescriptor"/>'s modes
/// (e.g. Player → "Local Player" / "By Name: Bob"). <see cref="Arg"/> holds the mode's
/// single argument as a display string, when the mode has one.
/// </summary>
public class TypedModeValueSource : ValueSource
{
    /// <summary>The macro type's <c>Type.FullName</c>.</summary>
    public string TypeId = "";
    public string ModeId = "";
    public string Arg;

    public override object Evaluate(MacroParam param, MacroRunContext ctx)
    {
        var type = MacroTypes.ById(TypeId) ?? MacroTypes.For(param.Type)
            ?? throw new InvalidOperationException($"No macro type registered for '{TypeId}' (plugin missing?).");
        var mode = type.FindMode(ModeId) ?? type.DefaultMode
            ?? throw new InvalidOperationException($"Macro type '{type.DisplayName}' has no selection modes.");

        var args = mode.Param != null
            ? new[] { MacroValues.Coerce(Arg, mode.Param.Type) }
            : Array.Empty<object>();
        return MacroValues.Coerce(mode.Resolve(args), param.Type);
    }
}

/// <summary>
/// Persists the <see cref="ValueSource"/> union as <c>{ "kind": "...", ...fields }</c>.
/// Unknown kinds load as empty constants instead of failing the whole macro file.
/// </summary>
public class ValueSourceConverter : JsonConverter
{
    private static readonly Dictionary<string, Type> _kinds = new()
    {
        ["constant"]   = typeof(ConstantValueSource),
        ["toggle"]     = typeof(ToggleValueSource),
        ["stepOutput"] = typeof(StepOutputValueSource),
        ["expr"]       = typeof(ExpressionValueSource),
        ["typedMode"]  = typeof(TypedModeValueSource),
    };

    /// <summary>Register an additional serializable kind (later phases / plugins).</summary>
    public static void RegisterKind(string kind, Type type) => _kinds[kind] = type;

    public override bool CanConvert(Type objectType) => typeof(ValueSource).IsAssignableFrom(objectType);

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null) return null;
        var obj  = JObject.Load(reader);
        var kind = obj.Value<string>("kind") ?? "constant";

        if (!_kinds.TryGetValue(kind, out var type))
        {
            MacroLog.Warn($"[Macros] Unknown value source kind '{kind}', loading as empty constant.");
            return new ConstantValueSource();
        }

        var result = (ValueSource)Activator.CreateInstance(type);
        obj.Remove("kind");
        serializer.Populate(obj.CreateReader(), result);
        return result;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var type = value.GetType();
        string kind = null;
        foreach (var kv in _kinds)
            if (kv.Value == type) { kind = kv.Key; break; }
        if (kind == null)
            throw new JsonSerializationException($"ValueSource type '{type.FullName}' has no registered kind.");

        var obj = new JObject { ["kind"] = kind };
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            var v = field.GetValue(value);
            var name = char.ToLowerInvariant(field.Name[0]) + field.Name.Substring(1);
            obj[name] = v == null ? JValue.CreateNull() : JToken.FromObject(v, serializer);
        }
        obj.WriteTo(writer);
    }
}
