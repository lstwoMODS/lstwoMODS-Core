using UnityEngine;
using SysColor = System.Drawing.Color;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

public readonly struct Col
{
    public readonly float R, G, B, A;
    public readonly float r, g, b, a;

    public Col(float r, float g, float b, float a = 1f)
    {
        R = r; G = g; B = b; A = a;
        this.r = r; this.g = g; this.b = b; this.a = a;
    }

    // System.Numerics conversions
    
    // Vector4 (RGBA)
    public static implicit operator Col(Vector4 v)
        => new(v.X, v.Y, v.Z, v.W);

    public static implicit operator Vector4(Col c)
        => new(c.R, c.G, c.B, c.A);

    // Vector3 (RGB only, alpha = 1)
    public static implicit operator Col(Vector3 v)
        => new(v.X, v.Y, v.Z, 1f);

    public static implicit operator Vector3(Col c)
        => new(c.R, c.G, c.B);

    // UnityEngine.Color conversions
    public static implicit operator Col(Color c)
        => new(c.r, c.g, c.b, c.a);

    public static implicit operator Color(Col c)
        => new(c.R, c.G, c.B, c.A);

    // System.Drawing.Color conversions
    public static implicit operator Col(SysColor c)
        => new(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);

    public static implicit operator SysColor(Col c)
        => SysColor.FromArgb(
            (byte)(c.A * 255f),
            (byte)(c.R * 255f),
            (byte)(c.G * 255f),
            (byte)(c.B * 255f)
        );

    // Unity vectors conversions

    // Vector3: R/G/B
    public static implicit operator Col(UnityEngine.Vector3 v)
        => new(v.x, v.y, v.z, 1f);

    public static implicit operator UnityEngine.Vector3(Col c)
        => new(c.R, c.G, c.B);

    // Vector4: R/G/B/A
    public static implicit operator Col(UnityEngine.Vector4 v)
        => new(v.x, v.y, v.z, v.w);

    public static implicit operator UnityEngine.Vector4(Col c)
        => new(c.R, c.G, c.B, c.A);
}
