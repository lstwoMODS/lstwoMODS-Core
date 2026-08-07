using System;
using System.Globalization;
using Newtonsoft.Json;

/// <summary>
/// Serializable stand-in for a 3D integer vector, the counterpart of <see cref="Vec3"/>.
/// Converts implicitly to and from <see cref="UnityEngine.Vector3Int"/>, and widens implicitly
/// to <see cref="Vec3"/>.
///
/// The components are readable as both <c>X</c>/<c>Y</c>/<c>Z</c> and their lower-case aliases.
/// Only the upper-case set is serialized.
/// </summary>
public readonly struct Vec3Int : IEquatable<Vec3Int>
{
    /// <summary>The X component.</summary>
    public readonly int X;
    /// <summary>The Y component.</summary>
    public readonly int Y;
    /// <summary>The Z component.</summary>
    public readonly int Z;

    /// <summary>Unity-style alias for <see cref="X"/>.</summary>
    [JsonIgnore] public int x => X;
    /// <summary>Unity-style alias for <see cref="Y"/>.</summary>
    [JsonIgnore] public int y => Y;
    /// <summary>Unity-style alias for <see cref="Z"/>.</summary>
    [JsonIgnore] public int z => Z;

    /// <summary>(0, 0, 0).</summary>
    public static Vec3Int Zero => new(0, 0, 0);
    /// <summary>(1, 1, 1).</summary>
    public static Vec3Int One => new(1, 1, 1);

    /// <param name="x">The X component.</param>
    /// <param name="y">The Y component.</param>
    /// <param name="z">The Z component.</param>
    [JsonConstructor]
    public Vec3Int(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>All three components set to <paramref name="value"/>.</summary>
    public Vec3Int(int value) : this(value, value, value) { }

    /// <summary>Splits into components, for <c>var (x, y, z) = vec;</c>.</summary>
    public void Deconstruct(out int x, out int y, out int z)
    {
        x = X;
        y = Y;
        z = Z;
    }

    // ── Conversions ──────────────────────────────────────────────────────

    /// <summary>Converts to <see cref="UnityEngine.Vector3Int"/>.</summary>
    public static implicit operator UnityEngine.Vector3Int(Vec3Int v)
        => new(v.X, v.Y, v.Z);

    /// <summary>Converts from <see cref="UnityEngine.Vector3Int"/>.</summary>
    public static implicit operator Vec3Int(UnityEngine.Vector3Int v)
        => new(v.x, v.y, v.z);

    /// <summary>Widens to <see cref="Vec3"/>.</summary>
    public static implicit operator Vec3(Vec3Int v)
        => new(v.X, v.Y, v.Z);

    /// <summary>Truncates a <see cref="Vec3"/> towards zero.</summary>
    public static explicit operator Vec3Int(Vec3 v)
        => new((int)v.X, (int)v.Y, (int)v.Z);

    // ── Arithmetic ───────────────────────────────────────────────────────

    /// <summary>Component-wise addition.</summary>
    public static Vec3Int operator +(Vec3Int a, Vec3Int b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    /// <summary>Component-wise subtraction.</summary>
    public static Vec3Int operator -(Vec3Int a, Vec3Int b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    /// <summary>Negation.</summary>
    public static Vec3Int operator -(Vec3Int v) => new(-v.X, -v.Y, -v.Z);
    /// <summary>Component-wise multiplication.</summary>
    public static Vec3Int operator *(Vec3Int a, Vec3Int b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
    /// <summary>Scales by <paramref name="s"/>.</summary>
    public static Vec3Int operator *(Vec3Int v, int s) => new(v.X * s, v.Y * s, v.Z * s);
    /// <summary>Scales by <paramref name="s"/>.</summary>
    public static Vec3Int operator *(int s, Vec3Int v) => new(v.X * s, v.Y * s, v.Z * s);

    // ── Equality ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public bool Equals(Vec3Int other) => X == other.X && Y == other.Y && Z == other.Z;

    /// <inheritdoc/>
    public override bool Equals(object obj) => obj is Vec3Int other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = X;
            hash = (hash * 397) ^ Y;
            hash = (hash * 397) ^ Z;
            return hash;
        }
    }

    /// <summary>Component-wise equality.</summary>
    public static bool operator ==(Vec3Int a, Vec3Int b) => a.Equals(b);
    /// <summary>Component-wise inequality.</summary>
    public static bool operator !=(Vec3Int a, Vec3Int b) => !a.Equals(b);

    /// <inheritdoc/>
    public override string ToString()
        => string.Format(CultureInfo.InvariantCulture, "({0}, {1}, {2})", X, Y, Z);
}
