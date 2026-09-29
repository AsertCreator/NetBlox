namespace NetBlox.Structs;

public record struct Color3
{
    public byte R;
    public byte G;
    public byte B;

    public Color3(byte r, byte g, byte b)
    {
        R = r;
        G = g;
        B = b;
    }

    public static Color3 FromBGRA(uint bgra)
    {
        Color3 color3 = new Color3();
        color3.B = (byte)((bgra >> 0) & 0xFF);
        color3.G = (byte)((bgra >> 8) & 0xFF);
        color3.R = (byte)((bgra >> 16) & 0xFF);
        return color3;
    }
    public static uint ToBGRA(Color3 color)
    {
        uint bgra = 0;
        bgra |= (uint)(color.B << 0);
        bgra |= (uint)(color.G << 8);
        bgra |= (uint)(color.R << 16);
        return bgra;
    }

    public static implicit operator Color3(Raylib_cs.Color color) => new Color3(color.R, color.G, color.B);
    public static implicit operator Raylib_cs.Color(Color3 color) => new Raylib_cs.Color(color.R, color.G, color.B);
}