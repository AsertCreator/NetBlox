using Raylib_cs;

namespace NetBlox.Rendering;

public class ClientRendererViewport : WorkspaceRendererViewport
{
    public ClientRendererViewport(GameRenderer gameRenderer) : base(gameRenderer)
    {
    }

    public override void RenderFrame()
    {
        base.RenderFrame();
    }
    public override void DoDebugKeys()
    {
        // NOP
    }
    public override void DrawGizmos(bool in3d)
    {
        // NOP
    }
}