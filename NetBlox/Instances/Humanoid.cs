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
            float x1 = viewport.MainCamera.Position.X;
            float y1 = viewport.MainCamera.Position.Z;
            float x2 = viewport.MainCamera.Target.X;
            float y2 = viewport.MainCamera.Target.Z;
            float cameraangle = MathF.Atan2(y2 - y1, x2 - x1);

            if (Raylib.IsKeyDown(KeyboardKey.Up) || Raylib.IsKeyDown(KeyboardKey.W))
            {
                totalTranslationX += WalkSpeed * MathF.Cos(cameraangle);
                totalTranslationZ += WalkSpeed * MathF.Sin(cameraangle);
            }
            if (Raylib.IsKeyDown(KeyboardKey.Left) || Raylib.IsKeyDown(KeyboardKey.A))
            {
                totalTranslationX += WalkSpeed * MathF.Cos(cameraangle - MathF.PI / 2);
                totalTranslationZ += WalkSpeed * MathF.Sin(cameraangle - MathF.PI / 2);
            }
            if (Raylib.IsKeyDown(KeyboardKey.Down) || Raylib.IsKeyDown(KeyboardKey.S))
            {
                totalTranslationX -= WalkSpeed * MathF.Cos(cameraangle);
                totalTranslationZ -= WalkSpeed * MathF.Sin(cameraangle);
            }
            if (Raylib.IsKeyDown(KeyboardKey.Right) || Raylib.IsKeyDown(KeyboardKey.D))
            {
                totalTranslationX -= WalkSpeed * MathF.Cos(cameraangle - MathF.PI / 2);
                totalTranslationZ -= WalkSpeed * MathF.Sin(cameraangle - MathF.PI / 2);
            }

            float ttlength = totalTranslationX * totalTranslationX + totalTranslationZ * totalTranslationZ;
            ttlength = MathF.Sqrt(ttlength);

            if (ttlength > 0)
            {
                totalTranslationX /= ttlength;
                totalTranslationZ /= ttlength;

                totalTranslationX *= WalkSpeed * (1f / GameManager.GameRenderer!.PreferredFPS) / 2;
                totalTranslationZ *= WalkSpeed * (1f / GameManager.GameRenderer!.PreferredFPS) / 2;

                Vector3 primaryForward = Raymath.Vector3RotateByQuaternion(Vector3.UnitX, model.PrimaryPart.QuaternionRotation);
                Vector2 idealLookatVector = -Vector2.Normalize(new Vector2(totalTranslationX, totalTranslationZ));
                float idealAngle = MathF.Atan2(-idealLookatVector.Y, idealLookatVector.X);
                float currentAngle = MathF.Atan2(-primaryForward.Z, primaryForward.X);
                
                if (ttlength > 0.001)
                {
                    float angleDifference = MathF.Atan2(
                        MathF.Sin(idealAngle - currentAngle),
                        MathF.Cos(idealAngle - currentAngle));
                    float rotationY = currentAngle + angleDifference / 6;

                    Quaternion quaternion = Raymath.QuaternionFromEuler(0, rotationY, 0);
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