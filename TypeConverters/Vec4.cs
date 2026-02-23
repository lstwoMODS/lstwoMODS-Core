using UnityEngine;
using Vector4 = System.Numerics.Vector4;

public readonly struct Vec4(float _x, float _y, float _z, float _w)
{
    public readonly float X = _x, Y = _y, Z = _z, W = _w;
    public readonly float x = _x, y = _y, z = _z, w = _w;

    // System.Numerics.Vector4
    public static implicit operator Vec4(Vector4 v)
        => new(v.X, v.Y, v.Z, v.W);

    public static implicit operator Vector4(Vec4 v)
        => new(v.X, v.Y, v.Z, v.W);

    // UnityEngine.Vector4
    public static implicit operator UnityEngine.Vector4(Vec4 v)
        => new(v.X, v.Y, v.Z, v.W);

    public static implicit operator Vec4(UnityEngine.Vector4 v)
        => new(v.x, v.y, v.z, v.w);
}