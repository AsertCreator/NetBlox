namespace NetBlox.Rendering;

public abstract class RendererViewport
{
    protected GameRenderer GameRenderer;

    public RendererViewport(GameRenderer gameRenderer)
    {
        GameRenderer = gameRenderer;
    }

    public abstract void RenderFrame();
}