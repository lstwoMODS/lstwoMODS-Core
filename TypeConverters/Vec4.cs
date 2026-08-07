using System;
using System.Globalization;
using Newtonsoft.Json;
using UnityEngine;
using Vector4 = System.Numerics.Vector4;

/// <summary>
/// Serializable stand-in for a 4D float vector. See <see cref="Vec2"/> for why Unity's own
/// vector types cannot be persisted directly. For colours prefer <see cref="Col"/>, which
/// carries the same four floats but names them R/G/B/A.
///
/// The components are readable as both <c>X</c>/<c>Y</c>/<c>Z</c>/<c>W</c> and their lower-case
/// aliases. Only the upper-case set is serialized.
/// </summary>
public readonly struct Vec4 : IEquatable<Vec4>
{
    /// <summary>The X component.</summary>
    public readonly float X;
    /// <summary>The Y component.</summary>
    public readonly float Y;
    /// <summary>The Z component.</summary>
    public readonly float Z;
    /// <summary>The W component.</summary>
    public readonly float W;

    /// <summary>Unity-style alias for <see cref="X"/>.</summary>
    [JsonIgnore] public float x => X;
    /// <summary>Unity-style alias for <see cref="Y"/>.</summary>
    [JsonIgnore] public float y => Y;
    /// <summary>Unity-style alias for <see cref="Z"/>.</summary>
    [JsonIgnore] public float z => Z;
    /// <summary>Unity-style alias for <see cref="W"/>.</summary>
    [JsonIgnore] public float w => W;

    /// <summary>(0, 0, 0, 0).</summary>
    public static Vec4 Zero => new(0f, 0f, 0f, 0f);
    /// <summary>(1, 1, 1, 1).</summary>
    public static Vec4 One => new(1f, 1f, 1f, 1f);

    /// <param name="x">The X component.</param>
    /// <param name="y">The Y component.</param>
    /// <param name="z">The Z component.</param>
    /// <param name="w">The W component.</param>
    [JsonConstructor]
    public Vec4(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    /// <summary>All four components set to <paramref name="value"/>.</summary>
    public Vec4(float value) : this(value, value, value, value) { }

    /// <summary>Splits into components, for <c>var (x, y, z, w) = vec;</c>.</summary>
    public void Deconstruct(out float x, out float y, out float z, out float w)
    {
        x = X;
        y = Y;
        z = Z;
        w = W;
    }

    // ── System.Numerics.Vector4 ──────────────────────────────────────────

    /// <summary>Converts from <see cref="System.Numerics.Vector4"/>.</summary>
    public static implicit operator Vec4(Vector4 v)
        => new(v.X, v.Y, v.Z, v.W);

    /// <summary>Converts to <see cref="System.Numerics.Vector4"/>.</summary>
    public static implicit operator Vector4(Vec4 v)
        => new(v.X, v.Y, v.Z, v.W);

    // ── UnityEngine.Vector4 ──────────────────────────────────────────────

    /// <summary>Converts to <see cref="UnityEngine.Vector4"/>.</summary>
    public static implicit operator UnityEngine.Vector4(Vec4 v)
        => new(v.X, v.Y, v.Z, v.W);

    /// <summary>Converts from <see cref="UnityEngine.Vector4"/>.</summary>
    public static implicit operator Vec4(UnityEngine.Vector4 v)
        => new(v.x, v.y, v.z, v.w);

    // ── Arithmetic ───────────────────────────────────────────────────────
    //
    // Declared here rather than left to the Unity/Numerics operators: with implicit
    // conversions to both of those, `a + b` on two Vec4 would otherwise be ambiguous.

    /// <summary>Component-wise addition.</summary>
    public static Vec4 operator +(Vec4 a, Vec4 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
    /// <summary>Component-wise subtraction.</summary>
    public static Vec4 operator -(Vec4 a, Vec4 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);
    /// <summary>Negation.</summary>
    public static Vec4 operator -(Vec4 v) => new(-v.X, -v.Y, -v.Z, -v.W);
    /// <summary>Component-wise multiplication.</summary>
    public static Vec4 operator *(Vec4 a, Vec4 b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z, a.W * b.W);
    /// <summary>Scales by <paramref name="s"/>.</summary>
    public static Vec4 operator *(Vec4 v, float s) => new(v.X * s, v.Y * s, v.Z * s, v.W * s);
    /// <summary>Scales by <paramref name="s"/>.</summary>
    public static Vec4 operator *(float s, Vec4 v) => new(v.X * s, v.Y * s, v.Z * s, v.W * s);
    /// <summary>Component-wise division.</summary>
    public static Vec4 operator /(Vec4 a, Vec4 b) => new(a.X / b.X, a.Y / b.Y, a.Z / b.Z, a.W / b.W);
    /// <summary>Divides by <paramref name="s"/>.</summary>
    public static Vec4 operator /(Vec4 v, float s) => new(v.X / s, v.Y / s, v.Z / s, v.W / s);

    // ── Equality ─────────────────────────────────────────────────────────
    //
    // Exact, unlike Unity's approximate vector comparison: these back UI change
    // detection, where "differs at all" is what we want to react to.

    /// <inheritdoc/>
    public bool Equals(Vec4 other) => X == other.X && Y == other.Y && Z == other.Z && W == other.W;

    /// <inheritdoc/>
    public override bool Equals(object obj) => obj is Vec4 other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = X.GetHashCode();
            hash = (hash * 397) ^ Y.GetHashCode();
            hash = (hash * 397) ^ Z.GetHashCode();
            hash = (hash * 397) ^ W.GetHashCode();
            return hash;
        }
    }

    /// <summary>Exact component-wise equality.</summary>
    public static bool operator ==(Vec4 a, Vec4 b) => a.Equals(b);
    /// <summary>Exact component-wise inequality.</summary>
    public static bool operator !=(Vec4 a, Vec4 b) => !a.Equals(b);

    /// <inheritdoc/>
    public override string ToString()
        => string.Format(CultureInfo.InvariantCulture, "({0}, {1}, {2}, {3})", X, Y, Z, W);
}
