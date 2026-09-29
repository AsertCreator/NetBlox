using System.Numerics;

namespace NetBlox.Structs;

public record struct UDim2
{
    public UDim X;
    public UDim Y;

    public UDim2(UDim x, UDim y)
    {
        X = x;
        Y = y;
    }
    public UDim2(float xscale, float xoffset, float yscale, float yoffset)
    {
        X.Scale = xscale;
        X.Offset = xoffset;
        Y.Scale = yscale;
        Y.Offset = yoffset;
    }

    public Vector2 Resolve(Vector2 container)
    {
        return new Vector2(X.Resolve(container.X), Y.Resolve(container.Y));
    }

    public static UDim2 operator +(UDim2 a, UDim2 b)
    {
        return new UDim2(a.X + b.X, a.Y + b.Y);
    }
    public static UDim2 operator -(UDim2 a, UDim2 b)
    {
        return new UDim2(a.X - b.X, a.Y - b.Y);
    }
}