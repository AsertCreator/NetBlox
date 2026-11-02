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
    public DecalSpecification TopDecal;
    public DecalSpecification LeftDecal;
    public DecalSpecification RightDecal;
    public DecalSpecification BottomDecal;
    public DecalSpecification FrontDecal;
    public DecalSpecification BackDecal;
}