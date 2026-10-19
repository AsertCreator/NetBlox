using NetBlox.Instances.Parts;

namespace NetBlox.Rendering;

public class RenderingEventArgs
{
    public readonly WorkspaceRendererViewport Viewport;

    public RenderingEventArgs(WorkspaceRendererViewport viewport)
    {
        Viewport = viewport;
    }

    public void WritePart(BasePart part) => Viewport.WritePart(part);
}