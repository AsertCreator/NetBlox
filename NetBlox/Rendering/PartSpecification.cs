using NetBlox.Structs;

namespace NetBlox.Rendering;

public record struct PartSpecification
{
    public PartType Shape;
    public SurfaceType TopSurface;
    public SurfaceType LeftSurface;
    public SurfaceType RightSurface;
    public SurfaceType BottomSurface;
    public SurfaceType FrontSurface;
    public SurfaceType BackSurface;
}