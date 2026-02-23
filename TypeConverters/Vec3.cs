using UnityEngine;
using Vector3 = System.Numerics.Vector3;

public readonly struct Vec3(float _x, float _y, float _z)
{
    public readonly float X = _x, Y = _y, Z = _z;
    public readonly float x = _x, y = _y, z = _z;

    // System.Numerics.Vector3
    public static implicit operator Vec3(Vector3 v)
        => new(v.X, v.Y, v.Z);

    public static implicit operator Vector3(Vec3 v)
        => new(v.X, v.Y, v.Z);

    // UnityEngine.Vector3
    public static implicit operator UnityEngine.Vector3(Vec3 v)
        => new(v.X, v.Y, v.Z);

    public static implicit operator Vec3(UnityEngine.Vector3 v)
        => new(v.x, v.y, v.z);
}