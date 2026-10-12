using System.Numerics;

namespace NetBlox.Structs;

public record struct CoordinateFrame
{
    public Vector3 Position;
    public Matrix4x4 Orientation;
}