using System.Numerics;
using NetBlox.Structs;

namespace NetBlox.Rendering;

public record struct PartSpecification
{
    public Vector3 Size;
    public PartType Shape;
    public Color3 Color;
    public SurfaceType TopSurface;
    public SurfaceType LeftSurface;
    public SurfaceType RightSurface;
    public SurfaceType BottomSurface;
    public SurfaceType FrontSurface;
    public SurfaceType BackSurface;
}