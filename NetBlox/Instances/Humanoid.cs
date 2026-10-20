using System.Numerics;
using NetBlox.Instances.Parts;
using NetBlox.Instances.Services;
using NetBlox.Rendering;
using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;
using Model = NetBlox.Instances.Parts.Model;

namespace NetBlox.Instances;

[Creatable]
public class Humanoid : Instance
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public float Health { get; set; } = 100;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public float MaxHealth { get; set; } = 100;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public float WalkSpeed { get; set; } = 16;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public float JumpPower { get; set; } = 16;

    public override string ClassName => nameof(Humanoid);

    public Humanoid(ulong id, GameManager gameManager) : base(id, gameManager)
    {
        RegisterForEventId(GameEvent.EVENT_RENDERGUI_LEVEL0);
        WalkSpeed = Root.GetService<StarterPlayer>().CharacterWalkSpeed;
        JumpPower = Root.GetService<StarterPlayer>().CharacterJumpPower;
    }

    public override void OnRegisteredEvent(EngineEventArgs args)
    {
        base.OnRegisteredEvent(args);
        if (args.GameEvent.Id == GameEvent.EVENT_RENDERGUI_LEVEL0 && Parent is Model model && IsPartOfDataModel)
        {
            RenderingEventArgs? renderingEventArgs = args.EventData as RenderingEventArgs;
            if (renderingEventArgs == null)
                return;

            WorkspaceRendererViewport viewport = renderingEventArgs.Viewport;
            RenderName(viewport);

            model.PrimaryPart ??= model.FindFirstChild_Fast("HumanoidRootPart") as BasePart;
            if (model.PrimaryPart == null)
                return;

            float totalTranslationX = default;
            float totalTranslationZ = default;

            if (Raylib.IsKeyDown(KeyboardKey.Up))
                totalTranslationX += 1;
            if (Raylib.IsKeyDown(KeyboardKey.Left))
                totalTranslationZ += -1;
            if (Raylib.IsKeyDown(KeyboardKey.Down))
                totalTranslationX += -1;
            if (Raylib.IsKeyDown(KeyboardKey.Right))
                totalTranslationZ += 1;

            float ttlength = totalTranslationX * totalTranslationX + totalTranslationZ * totalTranslationZ;
            ttlength = MathF.Sqrt(ttlength);

            if (ttlength > 0)
            {
                totalTranslationX /= ttlength;
                totalTranslationZ /= ttlength;

                totalTranslationX *= WalkSpeed * (1f / GameManager.GameRenderer!.PreferredFPS) / 2;
                totalTranslationZ *= WalkSpeed * (1f / GameManager.GameRenderer!.PreferredFPS) / 2;

                Vector3 primaryForward = Raymath.Vector3RotateByQuaternion(Vector3.UnitX, model.PrimaryPart.QuaternionRotation);
                Vector2 idealLookatVector = new Vector2(totalTranslationX, totalTranslationZ);
                Vector2 currentLookatVector = new Vector2(primaryForward.X, primaryForward.Z);
                Vector2 globalForwardLookatVector = new Vector2(1, 0);

                idealLookatVector = -Vector2.Normalize(idealLookatVector);
                currentLookatVector = Vector2.Normalize(currentLookatVector);

                float idealAngle = MathF.Acos(Vector2.Dot(idealLookatVector, globalForwardLookatVector));
                float currentAngle = MathF.Acos(Vector2.Dot(currentLookatVector, globalForwardLookatVector));
                
                if (ttlength > 0.001)
                {
                    Vector3 rotation = model.PrimaryPart.Rotation;
                    rotation.Y = (currentAngle + idealAngle) / 2 * (180 / MathF.PI);
                    rotation.X = 0;
                    rotation.Z = 0;

                    rotation /= 180 / MathF.PI;
                    Quaternion quaternion = Raymath.QuaternionFromEuler(rotation.Z, rotation.Y, rotation.X);
                    CoordinateFrame coordinateFrame = default;
                    coordinateFrame.Orientation = Raymath.QuaternionToMatrix(quaternion);
                    coordinateFrame.Position = model.PrimaryPart.Position + new Vector3(totalTranslationX, 0, totalTranslationZ);

                    model.PivotTo(coordinateFrame);
                }
            }
        }
    }

    private void RenderName(WorkspaceRendererViewport viewport)
    {
        BasePart? basePart = Parent!.FindFirstChild_Fast("Head") as BasePart;
        if (basePart == null)
            return;
        
        Vector3 worldspace = basePart.Position + new Vector3(0, 1.25f, 0);
        Vector3 namedir = worldspace - viewport.MainCamera.Position;
        Vector3 targetdir = viewport.MainCamera.Target - viewport.MainCamera.Position;

        if (Vector3.Dot(namedir, targetdir) <= 0)
            return;

        Rlgl.EnableDepthTest();

        FontSpecification fontSpecification = GameManager.GameRenderer!.DefaultFontSpecification1_5em;

        if ((viewport.MainCamera.Position - worldspace).Length() > 16)
            fontSpecification = GameManager.GameRenderer!.DefaultFontSpecification;

        Font font = GameManager.GameRenderer.FontRegistry.LoadFontFromSpecification(fontSpecification);
        Vector2 absscreenspace = Raylib.GetWorldToScreen(worldspace, viewport.MainCamera);
        Vector2 screenspace = absscreenspace;

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

        float healthfraction = Health / MaxHealth;
        float healthbarscale = 1 / (viewport.MainCamera.Position - worldspace).Length();
        int healthbarwidth = 800;
        int healthbarheight = healthbarwidth / 20;

        if (healthfraction != 1)
        {
            Raylib.DrawRectangle(
                (int)(absscreenspace.X - healthbarwidth / 2 * healthbarscale), 
                (int)(absscreenspace.Y - fontSpecification.Size - healthbarheight * healthbarscale), 
                (int)(healthbarwidth * healthbarscale), (int)(healthbarheight * healthbarscale), Color.Maroon);
                
            Raylib.DrawRectangle(
                (int)(absscreenspace.X - healthbarwidth / 2 * healthbarscale), 
                (int)(absscreenspace.Y - fontSpecification.Size - healthbarheight * healthbarscale), 
                (int)(healthbarwidth * healthbarscale * healthfraction), (int)(healthbarheight * healthbarscale), Color.Green);
        }
        
        Rlgl.DisableDepthTest();
    }

    public override bool IsA(string className)
    {
        if (className != nameof(Humanoid))
            return base.IsA(className);
        return true;
    }
}