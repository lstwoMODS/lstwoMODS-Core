using System;
using System.Globalization;
using Newtonsoft.Json;
using UnityEngine;
using Vector3 = System.Numerics.Vector3;

/// <summary>
/// Serializable stand-in for a 3D float vector. See <see cref="Vec2"/> for why Unity's own
/// vector types cannot be persisted directly.
///
/// The components are readable as both <c>X</c>/<c>Y</c>/<c>Z</c> and <c>x</c>/<c>y</c>/<c>z</c>.
/// Only the upper-case set is serialized.
/// </summary>
public readonly struct Vec3 : IEquatable<Vec3>
{
    /// <summary>The X component.</summary>
    public readonly float X;
    /// <summary>The Y component.</summary>
    public readonly float Y;
    /// <summary>The Z component.</summary>
    public readonly float Z;

    /// <summary>Unity-style alias for <see cref="X"/>.</summary>
    [JsonIgnore] public float x => X;
    /// <summary>Unity-style alias for <see cref="Y"/>.</summary>
    [JsonIgnore] public float y => Y;
    /// <summary>Unity-style alias for <see cref="Z"/>.</summary>
    [JsonIgnore] public float z => Z;

    /// <summary>(0, 0, 0).</summary>
    public static Vec3 Zero => new(0f, 0f, 0f);
    /// <summary>(1, 1, 1).</summary>
    public static Vec3 One => new(1f, 1f, 1f);

    /// <param name="x">The X component.</param>
    /// <param name="y">The Y component.</param>
    /// <param name="z">The Z component.</param>
    [JsonConstructor]
    public Vec3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>All three components set to <paramref name="value"/>.</summary>
    public Vec3(float value) : this(value, value, value) { }

    /// <summary>Splits into components, for <c>var (x, y, z) = vec;</c>.</summary>
    public void Deconstruct(out float x, out float y, out float z)
    {
        x = X;
        y = Y;
        z = Z;
    }

    // ── System.Numerics.Vector3 ──────────────────────────────────────────

    /// <summary>Converts from <see cref="System.Numerics.Vector3"/>.</summary>
    public static implicit operator Vec3(Vector3 v)
        => new(v.X, v.Y, v.Z);

    /// <summary>Converts to <see cref="System.Numerics.Vector3"/>.</summary>
    public static implicit operator Vector3(Vec3 v)
        => new(v.X, v.Y, v.Z);

    // ── UnityEngine.Vector3 ──────────────────────────────────────────────

    /// <summary>Converts to <see cref="UnityEngine.Vector3"/>.</summary>
    public static implicit operator UnityEngine.Vector3(Vec3 v)
        => new(v.X, v.Y, v.Z);

    /// <summary>Converts from <see cref="UnityEngine.Vector3"/>.</summary>
    public static implicit operator Vec3(UnityEngine.Vector3 v)
        => new(v.x, v.y, v.z);

    // ── Arithmetic ───────────────────────────────────────────────────────
    //
    // Declared here rather than left to the Unity/Numerics operators: with implicit
    // conversions to both of those, `a + b` on two Vec3 would otherwise be ambiguous.

    /// <summary>Component-wise addition.</summary>
    public static Vec3 operator +(Vec3 a, Vec3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    /// <summary>Component-wise subtraction.</summary>
    public static Vec3 operator -(Vec3 a, Vec3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    /// <summary>Negation.</summary>
    public static Vec3 operator -(Vec3 v) => new(-v.X, -v.Y, -v.Z);
    /// <summary>Component-wise multiplication.</summary>
    public static Vec3 operator *(Vec3 a, Vec3 b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
    /// <summary>Scales by <paramref name="s"/>.</summary>
    public static Vec3 operator *(Vec3 v, float s) => new(v.X * s, v.Y * s, v.Z * s);
    /// <summary>Scales by <paramref name="s"/>.</summary>
    public static Vec3 operator *(float s, Vec3 v) => new(v.X * s, v.Y * s, v.Z * s);
    /// <summary>Component-wise division.</summary>
    public static Vec3 operator /(Vec3 a, Vec3 b) => new(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
    /// <summary>Divides by <paramref name="s"/>.</summary>
    public static Vec3 operator /(Vec3 v, float s) => new(v.X / s, v.Y / s, v.Z / s);

    // ── Equality ─────────────────────────────────────────────────────────
    //
    // Exact, unlike Unity's approximate vector comparison: these back UI change
    // detection, where "differs at all" is what we want to react to.

    /// <inheritdoc/>
    public bool Equals(Vec3 other) => X == other.X && Y == other.Y && Z == other.Z;

    /// <inheritdoc/>
    public override bool Equals(object obj) => obj is Vec3 other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = X.GetHashCode();
            hash = (hash * 397) ^ Y.GetHashCode();
            hash = (hash * 397) ^ Z.GetHashCode();
            return hash;
        }
    }

    /// <summary>Exact component-wise equality.</summary>
    public static bool operator ==(Vec3 a, Vec3 b) => a.Equals(b);
    /// <summary>Exact component-wise inequality.</summary>
    public static bool operator !=(Vec3 a, Vec3 b) => !a.Equals(b);

    /// <inheritdoc/>
    public override string ToString()
        => string.Format(CultureInfo.InvariantCulture, "({0}, {1}, {2})", X, Y, Z);
}
