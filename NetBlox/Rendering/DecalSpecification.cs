using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using NetBlox.Structs;
using Raylib_cs;

namespace NetBlox.Rendering;

public struct DecalSpecification
{
    public float DecalTransparency;
    public Color3 DecalTint;
    public Texture2D DecalTexture;

    // because c# record struct's default implementation is abysmal

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static bool operator ==(DecalSpecification a, DecalSpecification b)
    {
        return a.DecalTransparency == b.DecalTransparency &&
            a.DecalTexture.Id == b.DecalTexture.Id &&
            a.DecalTint == b.DecalTint;
    }
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static bool operator !=(DecalSpecification a, DecalSpecification b)
    {
        return a.DecalTransparency != b.DecalTransparency ||
            a.DecalTexture.Id != b.DecalTexture.Id ||
            a.DecalTint != b.DecalTint;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj == null || obj is not DecalSpecification decalSpecification)
            return false;
        return this == decalSpecification;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        HashCode hashCode = default;
        hashCode.Add(DecalTransparency);
        hashCode.Add(DecalTint);
        hashCode.Add(DecalTexture.Id);
        return hashCode.ToHashCode();
    }
}