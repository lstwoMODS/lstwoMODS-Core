using System;
using System.Globalization;
using Newtonsoft.Json;
using UnityEngine;
using Vector2 = System.Numerics.Vector2;

/// <summary>
/// Serializable stand-in for a 2D float vector.
///
/// Converts implicitly to and from <see cref="UnityEngine.Vector2"/> and
/// <see cref="System.Numerics.Vector2"/>, so it can be passed wherever either of those is
/// expected, while still surviving a JSON round trip. Unity's own vector and color types do
/// not: their derived instance properties (<c>normalized</c>, <c>linear</c>, ...) return the
/// same type, so Newtonsoft recurses into them forever. Persist this instead.
///
/// The components are readable as both <c>X</c>/<c>Y</c> and <c>x</c>/<c>y</c> so code written
/// against either convention reads the same. Only the upper-case pair is serialized.
/// </summary>
public readonly struct Vec2 : IEquatable<Vec2>
{
    /// <summary>The X component.</summary>
    public readonly float X;
    /// <summary>The Y component.</summary>
    public readonly float Y;

    /// <summary>Unity-style alias for <see cref="X"/>.</summary>
    [JsonIgnore] public float x => X;
    /// <summary>Unity-style alias for <see cref="Y"/>.</summary>
    [JsonIgnore] public float y => Y;

    /// <summary>(0, 0).</summary>
    public static Vec2 Zero => new(0f, 0f);
    /// <summary>(1, 1).</summary>
    public static Vec2 One => new(1f, 1f);

    /// <param name="x">The X component.</param>
    /// <param name="y">The Y component.</param>
    [JsonConstructor]
    public Vec2(float x, float y)
    {
        X = x;
        Y = y;
    }

    /// <summary>Both components set to <paramref name="value"/>.</summary>
    public Vec2(float value) : this(value, value) { }

    /// <summary>Splits into components, for <c>var (x, y) = vec;</c>.</summary>
    public void Deconstruct(out float x, out float y)
    {
        x = X;
        y = Y;
    }

    // ── System.Numerics.Vector2 ──────────────────────────────────────────

    /// <summary>Converts from <see cref="System.Numerics.Vector2"/>.</summary>
    public static implicit operator Vec2(Vector2 v)
        => new(v.X, v.Y);

    /// <summary>Converts to <see cref="System.Numerics.Vector2"/>.</summary>
    public static implicit operator Vector2(Vec2 v)
        => new(v.X, v.Y);

    // ── UnityEngine.Vector2 ──────────────────────────────────────────────

    /// <summary>Converts to <see cref="UnityEngine.Vector2"/>.</summary>
    public static implicit operator UnityEngine.Vector2(Vec2 v)
        => new(v.X, v.Y);

    /// <summary>Converts from <see cref="UnityEngine.Vector2"/>.</summary>
    public static implicit operator Vec2(UnityEngine.Vector2 v)
        => new(v.x, v.y);

    // ── Arithmetic ───────────────────────────────────────────────────────
    //
    // Declared here rather than left to the Unity/Numerics operators: with implicit
    // conversions to both of those, `a + b` on two Vec2 would otherwise be ambiguous.

    /// <summary>Component-wise addition.</summary>
    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y);
    /// <summary>Component-wise subtraction.</summary>
    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y);
    /// <summary>Negation.</summary>
    public static Vec2 operator -(Vec2 v) => new(-v.X, -v.Y);
    /// <summary>Component-wise multiplication.</summary>
    public static Vec2 operator *(Vec2 a, Vec2 b) => new(a.X * b.X, a.Y * b.Y);
    /// <summary>Scales by <paramref name="s"/>.</summary>
    public static Vec2 operator *(Vec2 v, float s) => new(v.X * s, v.Y * s);
    /// <summary>Scales by <paramref name="s"/>.</summary>
    public static Vec2 operator *(float s, Vec2 v) => new(v.X * s, v.Y * s);
    /// <summary>Component-wise division.</summary>
    public static Vec2 operator /(Vec2 a, Vec2 b) => new(a.X / b.X, a.Y / b.Y);
    /// <summary>Divides by <paramref name="s"/>.</summary>
    public static Vec2 operator /(Vec2 v, float s) => new(v.X / s, v.Y / s);

    // ── Equality ─────────────────────────────────────────────────────────
    //
    // Exact, unlike Unity's approximate vector comparison: these back UI change
    // detection, where "differs at all" is what we want to react to.

    /// <inheritdoc/>
    public bool Equals(Vec2 other) => X == other.X && Y == other.Y;

    /// <inheritdoc/>
    public override bool Equals(object obj) => obj is Vec2 other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            return (X.GetHashCode() * 397) ^ Y.GetHashCode();
        }
    }

    /// <summary>Exact component-wise equality.</summary>
    public static bool operator ==(Vec2 a, Vec2 b) => a.Equals(b);
    /// <summary>Exact component-wise inequality.</summary>
    public static bool operator !=(Vec2 a, Vec2 b) => !a.Equals(b);

    /// <inheritdoc/>
    public override string ToString()
        => string.Format(CultureInfo.InvariantCulture, "({0}, {1})", X, Y);
}
