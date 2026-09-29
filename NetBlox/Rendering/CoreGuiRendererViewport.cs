using System.Numerics;
using NetBlox.Structs;
using Raylib_cs;

namespace NetBlox.Rendering;

public class CoreGuiRendererViewport : RendererViewport
{
    private GameSchedulerPerfEntry[] lastperfEntry;

    public CoreGuiRendererViewport(GameRenderer gameRenderer) : base(gameRenderer)
    {
        lastperfEntry = new GameSchedulerPerfEntry[1];
    }
    public virtual string ConstructDebugString()
    {
        return "NetBlox" + 
            ", fps: " + Raylib.GetFPS() + 
            ", actor count: " + (GameRenderer.GameManager.PhysicsSolver != null ? GameRenderer.GameManager.PhysicsSolver.GetActorCount() : 0) + 
            ", instance count: " + GameRenderer.GameManager.GameRegistry.GetInstanceCount();
    }
    public virtual void RenderDebugString()
    {
        Font font = GameRenderer.FontRegistry.LoadFontFromSpecification(GameRenderer.DefaultFontSpecification);
        Raylib.DrawTextEx(font, ConstructDebugString(), new Vector2(0, 0), GameRenderer.DefaultFontSpecification.Size, 0, Color.White);
    }
    public virtual void RenderDebugInfo()
    {
        Font font = GameRenderer.FontRegistry.LoadFontFromSpecification(GameRenderer.DefaultFontSpecification);

        RenderDebugString();

        if (lastperfEntry.Length - 1 < GameRenderer.GameManager.GameScheduler.SchedulerTasks.Count)
            Array.Resize(ref lastperfEntry, GameRenderer.GameManager.GameScheduler.SchedulerTasks.Count + 1);

        int count = GameRenderer.GameManager.GameScheduler.ReadPerformance(lastperfEntry);

        float totalMicroseconds = 0;
        for (int i = 0; i < count; i++)
            totalMicroseconds += lastperfEntry[i].TimeUsed;

        Vector2 vector2 = new Vector2(Raylib.GetScreenWidth() - 150, 150);
        
        float ms = 0;
        for (int i = 0; i < count; i++)
        {
            BrickColor brickColor = BrickColor.MediumStoneGrey;
            if (lastperfEntry[i].Task != null)
                brickColor = BrickColor.AllBrickColors[lastperfEntry[i].Task!.GetHashCode() % BrickColor.AllBrickColors.Length];
            
            string name = "Idle";
            if (lastperfEntry[i].Task != null)
                name = lastperfEntry[i].Task!.DebugName;

            Raylib.DrawCircleSector(vector2, 130, 
                ms / (float)totalMicroseconds * 360,
                (ms + lastperfEntry[i].TimeUsed) / (float)totalMicroseconds * 360, 18, brickColor.Color3);

            ms += lastperfEntry[i].TimeUsed;

            Raylib.DrawTextEx(font, name + " " + (lastperfEntry[i].TimeUsed / totalMicroseconds * 100) + "%", 
                new Vector2(vector2.X - 130, vector2.Y + 140 + i * 18), 16, 0, brickColor.Color3);
        }
    }
    public override void RenderFrame()
    {
        Raylib.ClearBackground(Color.Black);

        GameRenderer.GameManager.TryGetEventForId(GameEvent.EVENT_RENDERGUI_LEVEL0)?.Fire();
        GameRenderer.GameManager.TryGetEventForId(GameEvent.EVENT_RENDERGUI_LEVEL1)?.Fire();
        GameRenderer.GameManager.TryGetEventForId(GameEvent.EVENT_RENDERGUI_LEVEL2)?.Fire();
        GameRenderer.GameManager.TryGetEventForId(GameEvent.EVENT_RENDERGUI_LEVEL3)?.Fire();

        if (GameRenderer.DebugFlag)
            RenderDebugInfo();

        // Raylib.DrawTexture(ShadowMap.Texture, 0, 0, Color.White);
    }
}