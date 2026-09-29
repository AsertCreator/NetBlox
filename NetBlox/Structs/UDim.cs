namespace NetBlox.Structs;

public record struct UDim
{
    public float Scale;
    public float Offset;

    public UDim(float scale, float offset)
    {
        Scale = scale;
        Offset = offset;
    }

    public float Resolve(float containerAxis)
    {
        return containerAxis * Scale + Offset;
    }

    public static UDim operator +(UDim a, UDim b)
    {
        return new UDim(a.Scale + b.Scale, a.Offset + b.Offset);
    }
    public static UDim operator -(UDim a, UDim b)
    {
        return new UDim(a.Scale - b.Scale, a.Offset - b.Offset);
    }
}