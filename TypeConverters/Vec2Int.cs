using System;
using System.Globalization;
using Newtonsoft.Json;

/// <summary>
/// Serializable stand-in for a 2D integer vector, the counterpart of <see cref="Vec2"/>.
/// Converts implicitly to and from <see cref="UnityEngine.Vector2Int"/>, and widens implicitly
/// to <see cref="Vec2"/>.
///
/// The components are readable as both <c>X</c>/<c>Y</c> and <c>x</c>/<c>y</c>. Only the
/// upper-case pair is serialized.
/// </summary>
public readonly struct Vec2Int : IEquatable<Vec2Int>
{
    /// <summary>The X component.</summary>
    public readonly int X;
    /// <summary>The Y component.</summary>
    public readonly int Y;

    /// <summary>Unity-style alias for <see cref="X"/>.</summary>
    [JsonIgnore] public int x => X;
    /// <summary>Unity-style alias for <see cref="Y"/>.</summary>
    [JsonIgnore] public int y => Y;

    /// <summary>(0, 0).</summary>
    public static Vec2Int Zero => new(0, 0);
    /// <summary>(1, 1).</summary>
    public static Vec2Int One => new(1, 1);

    /// <param name="x">The X component.</param>
    /// <param name="y">The Y component.</param>
    [JsonConstructor]
    public Vec2Int(int x, int y)
    {
        X = x;
        Y = y;
    }

    /// <summary>Both components set to <paramref name="value"/>.</summary>
    public Vec2Int(int value) : this(value, value) { }

    /// <summary>Splits into components, for <c>var (x, y) = vec;</c>.</summary>
    public void Deconstruct(out int x, out int y)
    {
        x = X;
        y = Y;
    }

    // ── Conversions ──────────────────────────────────────────────────────

    /// <summary>Converts to <see cref="UnityEngine.Vector2Int"/>.</summary>
    public static implicit operator UnityEngine.Vector2Int(Vec2Int v)
        => new(v.X, v.Y);

    /// <summary>Converts from <see cref="UnityEngine.Vector2Int"/>.</summary>
    public static implicit operator Vec2Int(UnityEngine.Vector2Int v)
        => new(v.x, v.y);

    /// <summary>Widens to <see cref="Vec2"/>.</summary>
    public static implicit operator Vec2(Vec2Int v)
        => new(v.X, v.Y);

    /// <summary>Truncates a <see cref="Vec2"/> towards zero.</summary>
    public static explicit operator Vec2Int(Vec2 v)
        => new((int)v.X, (int)v.Y);

    // ── Arithmetic ───────────────────────────────────────────────────────

    /// <summary>Component-wise addition.</summary>
    public static Vec2Int operator +(Vec2Int a, Vec2Int b) => new(a.X + b.X, a.Y + b.Y);
    /// <summary>Component-wise subtraction.</summary>
    public static Vec2Int operator -(Vec2Int a, Vec2Int b) => new(a.X - b.X, a.Y - b.Y);
    /// <summary>Negation.</summary>
    public static Vec2Int operator -(Vec2Int v) => new(-v.X, -v.Y);
    /// <summary>Component-wise multiplication.</summary>
    public static Vec2Int operator *(Vec2Int a, Vec2Int b) => new(a.X * b.X, a.Y * b.Y);
    /// <summary>Scales by <paramref name="s"/>.</summary>
    public static Vec2Int operator *(Vec2Int v, int s) => new(v.X * s, v.Y * s);
    /// <summary>Scales by <paramref name="s"/>.</summary>
    public static Vec2Int operator *(int s, Vec2Int v) => new(v.X * s, v.Y * s);

    // ── Equality ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public bool Equals(Vec2Int other) => X == other.X && Y == other.Y;

    /// <inheritdoc/>
    public override bool Equals(object obj) => obj is Vec2Int other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            return (X * 397) ^ Y;
        }
    }

    /// <summary>Component-wise equality.</summary>
    public static bool operator ==(Vec2Int a, Vec2Int b) => a.Equals(b);
    /// <summary>Component-wise inequality.</summary>
    public static bool operator !=(Vec2Int a, Vec2Int b) => !a.Equals(b);

    /// <inheritdoc/>
    public override string ToString()
        => string.Format(CultureInfo.InvariantCulture, "({0}, {1})", X, Y);
}
