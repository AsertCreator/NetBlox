using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using NetBlox.Structs;

namespace NetBlox.Rendering;

public struct PartSpecification
{
    public PartType Shape;
    public SurfaceType TopSurface;
    public SurfaceType LeftSurface;
    public SurfaceType RightSurface;
    public SurfaceType BottomSurface;
    public SurfaceType FrontSurface;
    public SurfaceType BackSurface;
    public DecalSpecification TopDecal;
    public DecalSpecification LeftDecal;
    public DecalSpecification RightDecal;
    public DecalSpecification BottomDecal;
    public DecalSpecification FrontDecal;
    public DecalSpecification BackDecal;

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static bool operator ==(PartSpecification a, PartSpecification b)
    {
        return a.Shape == b.Shape &&
            a.TopSurface == b.TopSurface &&
            a.LeftSurface == b.LeftSurface &&
            a.RightSurface == b.RightSurface &&
            a.BottomSurface == b.BottomSurface &&
            a.FrontSurface == b.FrontSurface &&
            a.BackSurface == b.BackSurface &&
            a.TopDecal == b.TopDecal &&
            a.LeftDecal == b.LeftDecal &&
            a.RightDecal == b.RightDecal &&
            a.BottomDecal == b.BottomDecal &&
            a.FrontDecal == b.FrontDecal &&
            a.BackDecal == b.BackDecal;
    }
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static bool operator !=(PartSpecification a, PartSpecification b)
    {
        return a.Shape != b.Shape ||
            a.TopSurface != b.TopSurface ||
            a.LeftSurface != b.LeftSurface ||
            a.RightSurface != b.RightSurface ||
            a.BottomSurface != b.BottomSurface ||
            a.FrontSurface != b.FrontSurface ||
            a.BackSurface != b.BackSurface ||
            a.TopDecal != b.TopDecal ||
            a.LeftDecal != b.LeftDecal ||
            a.RightDecal != b.RightDecal ||
            a.BottomDecal != b.BottomDecal ||
            a.FrontDecal != b.FrontDecal ||
            a.BackDecal != b.BackDecal;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj == null || obj is not PartSpecification partSpecification)
            return false;
        return this == partSpecification;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        HashCode hashCode = default;
        hashCode.Add(Shape);
        hashCode.Add(TopSurface);
        hashCode.Add(LeftSurface);
        hashCode.Add(RightSurface);
        hashCode.Add(BottomSurface);
        hashCode.Add(FrontSurface);
        hashCode.Add(BackSurface);
        hashCode.Add(TopDecal.GetHashCode());
        hashCode.Add(LeftDecal.GetHashCode());
        hashCode.Add(RightDecal.GetHashCode());
        hashCode.Add(BottomDecal.GetHashCode());
        hashCode.Add(FrontDecal.GetHashCode());
        hashCode.Add(BackDecal.GetHashCode());
        return hashCode.ToHashCode();
    }
}