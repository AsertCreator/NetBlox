using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace NetBlox.Structs;

public struct Color3
{
    public byte R;
    public byte G;
    public byte B;

    public Color3(byte r, byte g, byte b)
    {
        R = r;
        G = g;
        B = b;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static Color3 FromBGRA(uint bgra)
    {
        Color3 color3 = default;
        color3.B = (byte)((bgra >> 0) & 0xFF);
        color3.G = (byte)((bgra >> 8) & 0xFF);
        color3.R = (byte)((bgra >> 16) & 0xFF);
        return color3;
    }
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static uint ToBGRA(Color3 color)
    {
        uint bgra = 0;
        bgra |= (uint)(color.B << 0);
        bgra |= (uint)(color.G << 8);
        bgra |= (uint)(color.R << 16);
        return bgra;
    }
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static Color3 FromARGBuint8(uint argb)
    {
        Color3 color3 = default;
        color3.R = (byte)((argb >> 16) & 0xFF);
        color3.G = (byte)((argb >> 8) & 0xFF);
        color3.B = (byte)((argb >> 0) & 0xFF);
        return color3;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public Vector3 ToVector3()
    {
        Vector3 vector3 = default;

        vector3.X = R / 255f;
        vector3.Y = G / 255f;
        vector3.Z = B / 255f;

        return vector3;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static bool operator ==(Color3 a, Color3 b)
    {
        return ToBGRA(a) == ToBGRA(b);
    }
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static bool operator !=(Color3 a, Color3 b)
    {
        return ToBGRA(a) != ToBGRA(b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj == null || obj is not Color3 color3)
            return false;
        return this == color3;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        return unchecked((int)ToBGRA(this));
    }

    public static implicit operator Color3(Raylib_cs.Color color) => new Color3(color.R, color.G, color.B);
    public static implicit operator Raylib_cs.Color(Color3 color) => new Raylib_cs.Color(color.R, color.G, color.B);
}