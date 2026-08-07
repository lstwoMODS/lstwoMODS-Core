using System;
using System.Globalization;
using Newtonsoft.Json;

/// <summary>
/// Serializable stand-in for a 4D integer vector, the counterpart of <see cref="Vec4"/>.
/// Unity has no <c>Vector4Int</c>, so this also converts to and from the
/// <c>(int, int, int, int)</c> tuple the four-component integer widgets used to expose.
///
/// The components are readable as both <c>X</c>/<c>Y</c>/<c>Z</c>/<c>W</c> and their lower-case
/// aliases. Only the upper-case set is serialized.
/// </summary>
public readonly struct Vec4Int : IEquatable<Vec4Int>
{
    /// <summary>The X component.</summary>
    public readonly int X;
    /// <summary>The Y component.</summary>
    public readonly int Y;
    /// <summary>The Z component.</summary>
    public readonly int Z;
    /// <summary>The W component.</summary>
    public readonly int W;

    /// <summary>Unity-style alias for <see cref="X"/>.</summary>
    [JsonIgnore] public int x => X;
    /// <summary>Unity-style alias for <see cref="Y"/>.</summary>
    [JsonIgnore] public int y => Y;
    /// <summary>Unity-style alias for <see cref="Z"/>.</summary>
    [JsonIgnore] public int z => Z;
    /// <summary>Unity-style alias for <see cref="W"/>.</summary>
    [JsonIgnore] public int w => W;

    /// <summary>(0, 0, 0, 0).</summary>
    public static Vec4Int Zero => new(0, 0, 0, 0);
    /// <summary>(1, 1, 1, 1).</summary>
    public static Vec4Int One => new(1, 1, 1, 1);

    /// <param name="x">The X component.</param>
    /// <param name="y">The Y component.</param>
    /// <param name="z">The Z component.</param>
    /// <param name="w">The W component.</param>
    [JsonConstructor]
    public Vec4Int(int x, int y, int z, int w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    /// <summary>All four components set to <paramref name="value"/>.</summary>
    public Vec4Int(int value) : this(value, value, value, value) { }

    /// <summary>Splits into components, for <c>var (x, y, z, w) = vec;</c>.</summary>
    public void Deconstruct(out int x, out int y, out int z, out int w)
    {
        x = X;
        y = Y;
        z = Z;
        w = W;
    }

    // ── Conversions ──────────────────────────────────────────────────────

    /// <summary>Converts from a <c>(x, y, z, w)</c> tuple.</summary>
    public static implicit operator Vec4Int((int X, int Y, int Z, int W) t)
        => new(t.X, t.Y, t.Z, t.W);

    /// <summary>Converts to a <c>(x, y, z, w)</c> tuple.</summary>
    public static implicit operator (int X, int Y, int Z, int W)(Vec4Int v)
        => (v.X, v.Y, v.Z, v.W);

    /// <summary>Widens to <see cref="Vec4"/>.</summary>
    public static implicit operator Vec4(Vec4Int v)
        => new(v.X, v.Y, v.Z, v.W);

    /// <summary>Truncates a <see cref="Vec4"/> towards zero.</summary>
    public static explicit operator Vec4Int(Vec4 v)
        => new((int)v.X, (int)v.Y, (int)v.Z, (int)v.W);

    // ── Arithmetic ───────────────────────────────────────────────────────

    /// <summary>Component-wise addition.</summary>
    public static Vec4Int operator +(Vec4Int a, Vec4Int b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
    /// <summary>Component-wise subtraction.</summary>
    public static Vec4Int operator -(Vec4Int a, Vec4Int b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);
    /// <summary>Negation.</summary>
    public static Vec4Int operator -(Vec4Int v) => new(-v.X, -v.Y, -v.Z, -v.W);
    /// <summary>Component-wise multiplication.</summary>
    public static Vec4Int operator *(Vec4Int a, Vec4Int b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z, a.W * b.W);
    /// <summary>Scales by <paramref name="s"/>.</summary>
    public static Vec4Int operator *(Vec4Int v, int s) => new(v.X * s, v.Y * s, v.Z * s, v.W * s);
    /// <summary>Scales by <paramref name="s"/>.</summary>
    public static Vec4Int operator *(int s, Vec4Int v) => new(v.X * s, v.Y * s, v.Z * s, v.W * s);

    // ── Equality ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public bool Equals(Vec4Int other) => X == other.X && Y == other.Y && Z == other.Z && W == other.W;

    /// <inheritdoc/>
    public override bool Equals(object obj) => obj is Vec4Int other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = X;
            hash = (hash * 397) ^ Y;
            hash = (hash * 397) ^ Z;
            hash = (hash * 397) ^ W;
            return hash;
        }
    }

    /// <summary>Component-wise equality.</summary>
    public static bool operator ==(Vec4Int a, Vec4Int b) => a.Equals(b);
    /// <summary>Component-wise inequality.</summary>
    public static bool operator !=(Vec4Int a, Vec4Int b) => !a.Equals(b);

    /// <inheritdoc/>
    public override string ToString()
        => string.Format(CultureInfo.InvariantCulture, "({0}, {1}, {2}, {3})", X, Y, Z, W);
}
