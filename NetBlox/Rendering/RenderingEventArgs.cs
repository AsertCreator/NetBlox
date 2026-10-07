using NetBlox.Instances.Parts;

namespace NetBlox.Rendering;

public class RenderingEventArgs(WorkspaceRendererViewport viewport)
{
    public void WritePart(BasePart part) => viewport.WritePart(part);
}