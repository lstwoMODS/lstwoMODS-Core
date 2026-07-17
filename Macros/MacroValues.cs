using System;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace lstwoMODS_Core.Macros;

/// <summary>Type coercion and display helpers for macro argument values.</summary>
public static class MacroValues
{
    private static readonly System.Collections.Generic.Dictionary<Type, Func<string, object>> _resolvers = new();

    /// <summary>Register a string→value resolver for a custom type (e.g. player name →
    /// PlayerRef), used by <see cref="Coerce"/> for constants, expression results and step
    /// outputs. Registered automatically by <see cref="MacroTypes.Register"/>.</summary>
    public static void RegisterResolver(Type type, Func<string, object> fromString) => _resolvers[type] = fromString;

    /// <summary>
    /// Convert <paramref name="raw"/> (a UI-entered string, a JSON token from disk, or a
    /// live object) to <paramref name="target"/>. Silent coercions per the design doc:
    /// int↔float, anything→string, enum↔underlying int / name string. Anything else that
    /// doesn't fit throws, and the runner reports it as a step failure.
    /// </summary>
    public static object Coerce(object raw, Type target)
    {
        if (target == null || target == typeof(object)) return raw is JToken j ? j.ToString() : raw;

        var t = Nullable.GetUnderlyingType(target) ?? target;

        if (raw is JToken token)
        {
            if (token.Type == JTokenType.Null) raw = null;
            else if (token is JValue jv && jv.Value is string) raw = (string)jv.Value;
            else
            {
                try { return token.ToObject(t); }
                catch { raw = token.ToString(); }
            }
        }

        if (raw == null) return DefaultFor(t);
        if (t.IsInstanceOfType(raw)) return raw;

        if (t.IsEnum)
        {
            if (raw is string es) return Enum.Parse(t, es, ignoreCase: true);
            return Enum.ToObject(t, Convert.ToInt64(raw, CultureInfo.InvariantCulture));
        }

        if (t == typeof(string)) return Convert.ToString(raw, CultureInfo.InvariantCulture);

        if (raw is string s)
        {
            s = s.Trim();
            if (s.Length == 0 && t != typeof(string)) return DefaultFor(t); // cleared input field
            if (_resolvers.TryGetValue(t, out var resolver))
                return resolver(s);
            if (t == typeof(Vector2) || t == typeof(Vector3) || t == typeof(Color))
                return ParseUnityValue(s, t);
            if (t == typeof(bool))
            {
                if (s == "1") return true;
                if (s == "0") return false;
                return bool.Parse(s);
            }
            // "3.5" for an int parameter: parse as double first (int↔float is a silent coercion)
            if (t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte))
                return Convert.ChangeType(double.Parse(s, CultureInfo.InvariantCulture), t, CultureInfo.InvariantCulture);
            return Convert.ChangeType(s, t, CultureInfo.InvariantCulture);
        }

        return Convert.ChangeType(raw, t, CultureInfo.InvariantCulture);
    }

    /// <summary>String form of a stored constant for editing in the UI.</summary>
    public static string ToDisplay(object raw) => raw switch
    {
        null => "",
        JValue jv => Convert.ToString(jv.Value, CultureInfo.InvariantCulture) ?? "",
        JToken jt => jt.ToString(),
        Vector2 v2 => $"{F(v2.x)}, {F(v2.y)}",
        Vector3 v3 => $"{F(v3.x)}, {F(v3.y)}, {F(v3.z)}",
        Color c => $"{F(c.r)}, {F(c.g)}, {F(c.b)}, {F(c.a)}",
        _ => Convert.ToString(raw, CultureInfo.InvariantCulture) ?? "",
    };

    private static string F(float v) => v.ToString(CultureInfo.InvariantCulture);

    /// <summary>Parse a UI-entered Unity value: "x, y, z" (decoration like "(...)"/"RGBA(...)"
    /// tolerated, so <see cref="ToDisplay"/> and Unity's own ToString both round-trip),
    /// or "#RRGGBB"/"#RRGGBBAA" for colors.</summary>
    private static object ParseUnityValue(string s, Type t)
    {
        if (t == typeof(Color) && s.StartsWith("#"))
        {
            var hex = s.Substring(1);
            if (hex.Length != 6 && hex.Length != 8)
                throw new FormatException($"'{s}' is not a #RRGGBB or #RRGGBBAA color.");
            float Channel(int at) => int.Parse(hex.Substring(at, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255f;
            return new Color(Channel(0), Channel(2), Channel(4), hex.Length == 8 ? Channel(6) : 1f);
        }

        var body = s;
        var open = body.IndexOf('(');
        if (open >= 0) body = body.Substring(open + 1).TrimEnd(')');
        var parts = body.Split(',').Select(p => float.Parse(p.Trim(), CultureInfo.InvariantCulture)).ToArray();

        if (t == typeof(Vector2) && parts.Length == 2) return new Vector2(parts[0], parts[1]);
        if (t == typeof(Vector3) && parts.Length == 3) return new Vector3(parts[0], parts[1], parts[2]);
        if (t == typeof(Color) && (parts.Length == 3 || parts.Length == 4))
            return new Color(parts[0], parts[1], parts[2], parts.Length == 4 ? parts[3] : 1f);
        throw new FormatException($"'{s}' is not a valid {t.Name}; use \"x, y{(t == typeof(Vector2) ? "" : ", z")}\".");
    }

    public static object DefaultFor(Type t)
    {
        if (t == null || t == typeof(string)) return "";
        return t.IsValueType ? Activator.CreateInstance(t) : null;
    }

    /// <summary>Short human-readable type name for parameter labels.</summary>
    public static string FriendlyTypeName(Type t)
    {
        if (t == typeof(bool))   return "bool";
        if (t == typeof(int) || t == typeof(long) || t == typeof(short)) return "int";
        if (t == typeof(float) || t == typeof(double)) return "float";
        if (t == typeof(string)) return "string";
        if (t == typeof(Vector2)) return "vec2";
        if (t == typeof(Vector3)) return "vec3";
        if (t == typeof(Color))   return "color";
        var macroType = MacroTypes.For(t);
        if (macroType != null) return macroType.DisplayName;
        return t?.Name ?? "?";
    }
}
