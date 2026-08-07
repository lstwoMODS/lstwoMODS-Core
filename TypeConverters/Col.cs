using System;
using System.Globalization;
using Newtonsoft.Json;
using UnityEngine;
using SysColor = System.Drawing.Color;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

/// <summary>
/// Serializable stand-in for an RGBA colour, components in the usual 0..1 range.
///
/// Converts implicitly to and from <see cref="UnityEngine.Color"/>,
/// <see cref="System.Drawing.Color"/>, the System.Numerics vectors and Unity's vectors, so it
/// can be passed wherever any of those is expected, while still surviving a JSON round trip.
/// <see cref="UnityEngine.Color"/> cannot: its <c>linear</c> and <c>gamma</c> properties return
/// a colour, so Newtonsoft recurses into them forever. Persist this instead.
///
/// The channels are readable as both <c>R</c>/<c>G</c>/<c>B</c>/<c>A</c> and their lower-case
/// aliases, so code written against either convention reads the same. Only the upper-case set
/// is serialized.
/// </summary>
[JsonConverter(typeof(ColJsonConverter))]
public readonly struct Col : IEquatable<Col>
{
    /// <summary>The red channel.</summary>
    public readonly float R;
    /// <summary>The green channel.</summary>
    public readonly float G;
    /// <summary>The blue channel.</summary>
    public readonly float B;
    /// <summary>The alpha channel.</summary>
    public readonly float A;

    /// <summary>Unity-style alias for <see cref="R"/>.</summary>
    [JsonIgnore] public float r => R;
    /// <summary>Unity-style alias for <see cref="G"/>.</summary>
    [JsonIgnore] public float g => G;
    /// <summary>Unity-style alias for <see cref="B"/>.</summary>
    [JsonIgnore] public float b => B;
    /// <summary>Unity-style alias for <see cref="A"/>.</summary>
    [JsonIgnore] public float a => A;

    /// <summary>Opaque white.</summary>
    public static Col White => new(1f, 1f, 1f);
    /// <summary>Opaque black.</summary>
    public static Col Black => new(0f, 0f, 0f);
    /// <summary>Fully transparent black.</summary>
    public static Col Clear => new(0f, 0f, 0f, 0f);

    /// <param name="r">The red channel.</param>
    /// <param name="g">The green channel.</param>
    /// <param name="b">The blue channel.</param>
    /// <param name="a">The alpha channel. Defaults to fully opaque.</param>
    [JsonConstructor]
    public Col(float r, float g, float b, float a = 1f)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    /// <summary>Splits into channels, for <c>var (r, g, b, a) = col;</c>.</summary>
    public void Deconstruct(out float r, out float g, out float b, out float a)
    {
        r = R;
        g = G;
        b = B;
        a = A;
    }

    /// <summary>A copy of this colour with its alpha replaced.</summary>
    public Col WithAlpha(float alpha) => new(R, G, B, alpha);

    // ── System.Numerics conversions ──────────────────────────────────────

    /// <summary>Converts from <see cref="System.Numerics.Vector4"/> (RGBA).</summary>
    public static implicit operator Col(Vector4 v)
        => new(v.X, v.Y, v.Z, v.W);

    /// <summary>Converts to <see cref="System.Numerics.Vector4"/> (RGBA).</summary>
    public static implicit operator Vector4(Col c)
        => new(c.R, c.G, c.B, c.A);

    /// <summary>Converts from <see cref="System.Numerics.Vector3"/> (RGB, opaque).</summary>
    public static implicit operator Col(Vector3 v)
        => new(v.X, v.Y, v.Z, 1f);

    /// <summary>Converts to <see cref="System.Numerics.Vector3"/>, dropping alpha.</summary>
    public static implicit operator Vector3(Col c)
        => new(c.R, c.G, c.B);

    // ── UnityEngine.Color ────────────────────────────────────────────────

    /// <summary>Converts from <see cref="UnityEngine.Color"/>.</summary>
    public static implicit operator Col(Color c)
        => new(c.r, c.g, c.b, c.a);

    /// <summary>Converts to <see cref="UnityEngine.Color"/>.</summary>
    public static implicit operator Color(Col c)
        => new(c.R, c.G, c.B, c.A);

    // ── System.Drawing.Color ─────────────────────────────────────────────

    /// <summary>Converts from <see cref="System.Drawing.Color"/> (0..255 per channel).</summary>
    public static implicit operator Col(SysColor c)
        => new(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);

    /// <summary>Converts to <see cref="System.Drawing.Color"/>, clamping to 0..255.</summary>
    public static implicit operator SysColor(Col c)
        => SysColor.FromArgb(
            ToByte(c.A),
            ToByte(c.R),
            ToByte(c.G),
            ToByte(c.B)
        );

    /// <summary>Out-of-range channels would otherwise wrap when cast to a byte.</summary>
    private static int ToByte(float channel)
        => (int)(channel < 0f ? 0f : channel > 1f ? 255f : channel * 255f);

    // ── Unity vector conversions ─────────────────────────────────────────

    /// <summary>Converts from <see cref="UnityEngine.Vector3"/> (RGB, opaque).</summary>
    public static implicit operator Col(UnityEngine.Vector3 v)
        => new(v.x, v.y, v.z, 1f);

    /// <summary>Converts to <see cref="UnityEngine.Vector3"/>, dropping alpha.</summary>
    public static implicit operator UnityEngine.Vector3(Col c)
        => new(c.R, c.G, c.B);

    /// <summary>Converts from <see cref="UnityEngine.Vector4"/> (RGBA).</summary>
    public static implicit operator Col(UnityEngine.Vector4 v)
        => new(v.x, v.y, v.z, v.w);

    /// <summary>Converts to <see cref="UnityEngine.Vector4"/> (RGBA).</summary>
    public static implicit operator UnityEngine.Vector4(Col c)
        => new(c.R, c.G, c.B, c.A);

    // ── Vec3 / Vec4 ──────────────────────────────────────────────────────

    /// <summary>Converts from <see cref="Vec4"/> (RGBA).</summary>
    public static implicit operator Col(Vec4 v)
        => new(v.X, v.Y, v.Z, v.W);

    /// <summary>Converts to <see cref="Vec4"/> (RGBA).</summary>
    public static implicit operator Vec4(Col c)
        => new(c.R, c.G, c.B, c.A);

    /// <summary>Converts from <see cref="Vec3"/> (RGB, opaque).</summary>
    public static implicit operator Col(Vec3 v)
        => new(v.X, v.Y, v.Z, 1f);

    /// <summary>Converts to <see cref="Vec3"/>, dropping alpha.</summary>
    public static implicit operator Vec3(Col c)
        => new(c.R, c.G, c.B);

    // ── Equality ─────────────────────────────────────────────────────────
    //
    // Exact, unlike Unity's approximate colour comparison: this backs UI change
    // detection, where "differs at all" is what we want to react to.

    /// <inheritdoc/>
    public bool Equals(Col other) => R == other.R && G == other.G && B == other.B && A == other.A;

    /// <inheritdoc/>
    public override bool Equals(object obj) => obj is Col other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = R.GetHashCode();
            hash = (hash * 397) ^ G.GetHashCode();
            hash = (hash * 397) ^ B.GetHashCode();
            hash = (hash * 397) ^ A.GetHashCode();
            return hash;
        }
    }

    /// <summary>Exact channel-wise equality.</summary>
    public static bool operator ==(Col a, Col b) => a.Equals(b);
    /// <summary>Exact channel-wise inequality.</summary>
    public static bool operator !=(Col a, Col b) => !a.Equals(b);

    /// <inheritdoc/>
    public override string ToString()
        => string.Format(CultureInfo.InvariantCulture, "RGBA({0}, {1}, {2}, {3})", R, G, B, A);
}
