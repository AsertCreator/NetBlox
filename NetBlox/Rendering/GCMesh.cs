using Raylib_cs;

namespace NetBlox.Rendering;

public class GCMesh
{
    public Mesh Mesh;

    public GCMesh(Mesh mesh)
    {
        Mesh = mesh;
    }
    ~GCMesh()
    {
        Raylib.UnloadMesh(Mesh);
    }
}