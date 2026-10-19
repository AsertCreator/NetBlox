using System.Numerics;
using NetBlox.Instances.Parts;
using NetBlox.Rendering;
using NetBlox.Runtime;
using Raylib_cs;
using Model = NetBlox.Instances.Parts.Model;

namespace NetBlox.Instances;

[Creatable]
public class Humanoid : Instance
{
    public override string ClassName => nameof(Humanoid);

    public Humanoid(ulong id, GameManager gameManager) : base(id, gameManager)
    {
        RegisterForEventId(GameEvent.EVENT_RENDERGUI_LEVEL0);
    }

    public override void OnRegisteredEvent(EngineEventArgs args)
    {
        base.OnRegisteredEvent(args);
        if (args.GameEvent.Id == GameEvent.EVENT_RENDERGUI_LEVEL0 && Parent is Model && IsPartOfDataModel)
        {
            RenderingEventArgs? renderingEventArgs = args.EventData as RenderingEventArgs;
            if (renderingEventArgs == null)
                return;
            WorkspaceRendererViewport viewport = renderingEventArgs.Viewport;
            RenderName(viewport);
        }
    }

    private void RenderName(WorkspaceRendererViewport viewport)
    {
        BasePart? basePart = Parent!.FindFirstChild_Fast("Head") as BasePart;
        if (basePart == null)
            return;
        
        Vector3 worldspace = basePart.Position + new Vector3(0, 2, 0);
        Vector3 namedir = worldspace - viewport.MainCamera.Position;
        Vector3 targetdir = viewport.MainCamera.Target - viewport.MainCamera.Position;

        if (Vector3.Dot(namedir, targetdir) <= 0)
            return;

        Rlgl.EnableDepthTest();

        FontSpecification fontSpecification = GameManager.GameRenderer!.DefaultFontSpecification;
        Font font = GameManager.GameRenderer.FontRegistry.LoadFontFromSpecification(fontSpecification);
        Vector2 screenspace = Raylib.GetWorldToScreen(worldspace, viewport.MainCamera);

        screenspace -= Raylib.MeasureTextEx(font, Parent.Name, fontSpecification.Size, 0) / 2;

        Raylib.DrawTextEx(font, Parent.Name, screenspace + new Vector2(-1, 1), fontSpecification.Size, 0, Color.Black);
        Raylib.DrawTextEx(font, Parent.Name, screenspace + new Vector2(0, 1), fontSpecification.Size, 0, Color.Black);
        Raylib.DrawTextEx(font, Parent.Name, screenspace + new Vector2(1, 1), fontSpecification.Size, 0, Color.Black);
        Raylib.DrawTextEx(font, Parent.Name, screenspace + new Vector2(-1, 0), fontSpecification.Size, 0, Color.Black);
        Raylib.DrawTextEx(font, Parent.Name, screenspace + new Vector2(0, 0), fontSpecification.Size, 0, Color.Black);
        Raylib.DrawTextEx(font, Parent.Name, screenspace + new Vector2(1, 0), fontSpecification.Size, 0, Color.Black);
        Raylib.DrawTextEx(font, Parent.Name, screenspace + new Vector2(-1, -1), fontSpecification.Size, 0, Color.Black);
        Raylib.DrawTextEx(font, Parent.Name, screenspace + new Vector2(0, -1), fontSpecification.Size, 0, Color.Black);
        Raylib.DrawTextEx(font, Parent.Name, screenspace + new Vector2(1, -1), fontSpecification.Size, 0, Color.Black);

        Raylib.DrawTextEx(font, Parent.Name, screenspace, fontSpecification.Size, 0, Color.White);
        
        Rlgl.DisableDepthTest();
    }

    public override bool IsA(string className)
    {
        if (className != nameof(Humanoid))
            return base.IsA(className);
        return true;
    }
}