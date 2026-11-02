using Raylib_cs;

namespace NetBlox.Rendering;

public class GCTexture2D
{
    public Texture2D Texture2D;

    public GCTexture2D(Texture2D texture)
    {
        Texture2D = texture;
    }
    ~GCTexture2D()
    {
        Raylib.UnloadTexture(Texture2D);
    }
}