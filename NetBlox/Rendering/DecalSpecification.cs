using NetBlox.Structs;
using Raylib_cs;

namespace NetBlox.Rendering;

public record struct DecalSpecification
{
    public float DecalTransparency;
    public Color3 DecalTint;
    public Texture2D DecalTexture;
}