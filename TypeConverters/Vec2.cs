using UnityEngine;
using Vector2 = System.Numerics.Vector2;

public readonly struct Vec2(float _x, float _y)
{
    public readonly float X = _x, Y = _y;
    public readonly float x = _x, y = _y;

    // System.Numerics.Vector2
    public static implicit operator Vec2(Vector2 v)
        => new(v.X, v.Y);

    public static implicit operator Vector2(Vec2 v)
        => new(v.X, v.Y);

    // UnityEngine.Vector2
    public static implicit operator UnityEngine.Vector2(Vec2 v)
        => new(v.X, v.Y);

    public static implicit operator Vec2(UnityEngine.Vector2 v)
        => new(v.x, v.y);
}