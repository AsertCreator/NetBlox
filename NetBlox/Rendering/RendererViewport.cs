namespace NetBlox.Rendering;

public abstract class RendererViewport
{
    public readonly GameRenderer GameRenderer;

    public RendererViewport(GameRenderer gameRenderer)
    {
        GameRenderer = gameRenderer;
    }

    public abstract void RenderFrame();
}