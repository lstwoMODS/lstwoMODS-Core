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

        // Vector2 ⇄ Vec2, Color ⇄ Col, ... Expression helpers and live getters hand back
        // whichever flavour their author used; a parameter declared as the other one takes
        // it just as happily. Boxed values go through reflection, which will not apply the
        // implicit conversion for us.
        if (IsVectorLike(t) && TryConvertVectorLike(raw, t, out var sameShape)) return sameShape;

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
            if (IsVectorLike(t))
                return ParseVectorValue(s, t);
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
        Vector4 v4 => $"{F(v4.x)}, {F(v4.y)}, {F(v4.z)}, {F(v4.w)}",
        Color c => $"{F(c.r)}, {F(c.g)}, {F(c.b)}, {F(c.a)}",
        Vec2 v2 => $"{F(v2.X)}, {F(v2.Y)}",
        Vec3 v3 => $"{F(v3.X)}, {F(v3.Y)}, {F(v3.Z)}",
        Vec4 v4 => $"{F(v4.X)}, {F(v4.Y)}, {F(v4.Z)}, {F(v4.W)}",
        Col c => $"{F(c.R)}, {F(c.G)}, {F(c.B)}, {F(c.A)}",
        _ => Convert.ToString(raw, CultureInfo.InvariantCulture) ?? "",
    };

    private static string F(float v) => v.ToString(CultureInfo.InvariantCulture);

    /// <summary>Vector and color parameter types, in both the Unity and the serializable
    /// (<see cref="Vec2"/>, <see cref="Col"/>, ...) flavour. These share one display form,
    /// so a macro file written against either keeps loading.</summary>
    private static bool IsVectorLike(Type t)
        => t == typeof(Vector2) || t == typeof(Vector3) || t == typeof(Vector4) || t == typeof(Color)
        || t == typeof(Vec2)    || t == typeof(Vec3)    || t == typeof(Vec4)    || t == typeof(Col);

    /// <summary>Convert between the two flavours of the same shape, e.g. a boxed
    /// <see cref="Vector2"/> for a <see cref="Vec2"/> target. False when the shapes differ.</summary>
    private static bool TryConvertVectorLike(object raw, Type t, out object result)
    {
        result = raw switch
        {
            Vector2 v when t == typeof(Vec2)    => (object)(Vec2)v,
            Vec2 v    when t == typeof(Vector2) => (Vector2)v,
            Vector3 v when t == typeof(Vec3)    => (Vec3)v,
            Vec3 v    when t == typeof(Vector3) => (Vector3)v,
            Vector4 v when t == typeof(Vec4)    => (Vec4)v,
            Vec4 v    when t == typeof(Vector4) => (Vector4)v,
            Color v   when t == typeof(Col)     => (Col)v,
            Col v     when t == typeof(Color)   => (Color)v,
            _ => null,
        };
        return result != null;
    }

    /// <summary>Parse a UI-entered vector or color: "x, y, z" (decoration like "(...)"/"RGBA(...)"
    /// tolerated, so <see cref="ToDisplay"/> and Unity's own ToString both round-trip),
    /// or "#RRGGBB"/"#RRGGBBAA" for colors.</summary>
    private static object ParseVectorValue(string s, Type t)
    {
        var isColor = t == typeof(Color) || t == typeof(Col);

        if (isColor && s.StartsWith("#"))
        {
            var hex = s.Substring(1);
            if (hex.Length != 6 && hex.Length != 8)
                throw new FormatException($"'{s}' is not a #RRGGBB or #RRGGBBAA color.");
            float Channel(int at) => int.Parse(hex.Substring(at, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255f;
            var parsed = new Col(Channel(0), Channel(2), Channel(4), hex.Length == 8 ? Channel(6) : 1f);
            return t == typeof(Col) ? parsed : (Color)parsed;
        }

        var body = s;
        var open = body.IndexOf('(');
        if (open >= 0) body = body.Substring(open + 1).TrimEnd(')');
        var parts = body.Split(',').Select(p => float.Parse(p.Trim(), CultureInfo.InvariantCulture)).ToArray();

        if (t == typeof(Vector2) && parts.Length == 2) return new Vector2(parts[0], parts[1]);
        if (t == typeof(Vec2)    && parts.Length == 2) return new Vec2(parts[0], parts[1]);
        if (t == typeof(Vector3) && parts.Length == 3) return new Vector3(parts[0], parts[1], parts[2]);
        if (t == typeof(Vec3)    && parts.Length == 3) return new Vec3(parts[0], parts[1], parts[2]);
        if (t == typeof(Vector4) && parts.Length == 4) return new Vector4(parts[0], parts[1], parts[2], parts[3]);
        if (t == typeof(Vec4)    && parts.Length == 4) return new Vec4(parts[0], parts[1], parts[2], parts[3]);
        if (isColor && (parts.Length == 3 || parts.Length == 4))
        {
            var parsed = new Col(parts[0], parts[1], parts[2], parts.Length == 4 ? parts[3] : 1f);
            return t == typeof(Col) ? parsed : (Color)parsed;
        }

        throw new FormatException($"'{s}' is not a valid {t.Name}; use \"{ComponentHint(t)}\".");
    }

    /// <summary>The component list to show in a parse error, e.g. "x, y, z".</summary>
    private static string ComponentHint(Type t)
    {
        if (t == typeof(Vector2) || t == typeof(Vec2)) return "x, y";
        if (t == typeof(Vector3) || t == typeof(Vec3)) return "x, y, z";
        if (t == typeof(Color) || t == typeof(Col))    return "r, g, b, a";
        return "x, y, z, w";
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
        if (t == typeof(Vector2) || t == typeof(Vec2)) return "vec2";
        if (t == typeof(Vector3) || t == typeof(Vec3)) return "vec3";
        if (t == typeof(Vector4) || t == typeof(Vec4)) return "vec4";
        if (t == typeof(Color) || t == typeof(Col))    return "color";
        var macroType = MacroTypes.For(t);
        if (macroType != null) return macroType.DisplayName;
        return t?.Name ?? "?";
    }
}
