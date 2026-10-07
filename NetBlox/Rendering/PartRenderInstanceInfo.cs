using System.Numerics;
using Raylib_cs;

namespace NetBlox.Rendering;

public record struct PartRenderInstanceInfo
{
    public Float16 Transform;
    public Vector4 Color;
}