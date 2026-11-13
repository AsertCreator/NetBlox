using System.Numerics;
using Jitter2.Dynamics;
using NetBlox.Instances.Parts;
using NetBlox.Instances.Services;
using NetBlox.Rendering;
using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;
using Model = NetBlox.Instances.Parts.Model;

namespace NetBlox.Instances;

[Creatable]
[ExplorerIconIndex(9)]
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
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public Vector3 WalkToPoint
    {
        get => walkToPoint;
        set
        {
            if (value != walkToPoint)
            {
                walkToPoint = value;
                walkToPointDone = false;
            }
        }
    }

    private Vector3 walkToPoint;
    private bool walkToPointDone = true;

    public override string ClassName => nameof(Humanoid);

    public Humanoid(ulong id, GameManager gameManager) : base(id, gameManager)
    {
        RegisterForEventId(GameEvent.EVENT_RENDERGUI_LEVEL0);
        RegisterForEventId(GameEvent.EVENT_BEFORE_PHYSICS);
        
        WalkSpeed = Root.GetService<StarterPlayer>().CharacterWalkSpeed;
        JumpPower = Root.GetService<StarterPlayer>().CharacterJumpPower;
    }

    public override void OnRegisteredEvent(EngineEventArgs args)
    {
        base.OnRegisteredEvent(args);

        if (Parent is not Model model || !IsPartOfDataModel)
            return;

        model.PrimaryPart ??= model.FindFirstChild_Fast("HumanoidRootPart") as BasePart;

        if (args.GameEvent.Id == GameEvent.EVENT_BEFORE_PHYSICS && model.PrimaryPart != null)
        {
            KeepUpright(model.PrimaryPart);
            ApplyHumanoidMovement();
            FixVelocities();
        }
        else if (args.GameEvent.Id == GameEvent.EVENT_RENDERGUI_LEVEL0)
        {
            RenderingEventArgs? renderingEventArgs = args.EventData as RenderingEventArgs;
            if (renderingEventArgs == null)
                return;

            WorkspaceRendererViewport viewport = renderingEventArgs.Viewport;

            RenderName(viewport);
            RenderHealthbar(viewport);
        }
    }

    private void ApplyHumanoidMovement()
    {
        if (Parent is not Model model || !IsPartOfDataModel)
            return;
        if (model.PrimaryPart == null)
            return;

        if (!walkToPointDone)
        {
            Vector2 tdPrimaryPartPos = new Vector2(model.PrimaryPart.Position.X, model.PrimaryPart.Position.Z);
            Vector2 tdWalkToPointPos = new Vector2(walkToPoint.X, walkToPoint.Z);
            float totalTranslationX = Vector2.Normalize(tdWalkToPointPos - tdPrimaryPartPos).X * WalkSpeed;
            float totalTranslationZ = Vector2.Normalize(tdWalkToPointPos - tdPrimaryPartPos).Y * WalkSpeed;
            float ttlength = totalTranslationX * totalTranslationX + totalTranslationZ * totalTranslationZ;

            ttlength = MathF.Sqrt(ttlength);

            if ((tdPrimaryPartPos - tdWalkToPointPos).Length() < 0.001f)
            {
                walkToPointDone = true;
            }
            else if (ttlength > 0)
            {
                totalTranslationX /= ttlength;
                totalTranslationZ /= ttlength;

                totalTranslationX *= WalkSpeed * (1f / GameManager.GameRenderer!.PreferredFPS);
                totalTranslationZ *= WalkSpeed * (1f / GameManager.GameRenderer!.PreferredFPS);

                Vector3 primaryForward = Vector3.Transform(Vector3.UnitX, model.PrimaryPart.QuaternionRotation);
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
                    coordinateFrame.Position = model.PrimaryPart.Position;

                    Vector3 newtranslation = model.TransformDisplacementForCollisionalSliding(new Vector3(totalTranslationX, 0, totalTranslationZ));
                    coordinateFrame.Position += newtranslation;

                    model.PivotTo(coordinateFrame);
                }
            }
        }
    }

    private void FixVelocities()
    {
        if (Parent is not Model model || !IsPartOfDataModel)
            return;
        if (model.PrimaryPart == null)
            return;

        model.PrimaryPart.Velocity = new Vector3(
            model.PrimaryPart.Velocity.X / 2,
            model.PrimaryPart.Velocity.Y,
            model.PrimaryPart.Velocity.Z / 2
        );
        model.PrimaryPart.AngularVelocity /= 16;
    }

    private void KeepUpright(BasePart rootPart)
    {
        MotionAxes allowedMotion = MotionAxes.Linear | MotionAxes.AngularY;
        if (rootPart.OwnPhysicsActor?.RigidBody != null)
            rootPart.OwnPhysicsActor?.RigidBody.AllowedMotion = allowedMotion;
        if (rootPart.PhysicsAssembly != null)
            rootPart.PhysicsAssembly.CombinedRigidBody.AllowedMotion = allowedMotion;
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

        FontSpecification fontSpecification = GameManager.GameRenderer!.DefaultFontSpecification1_5em;

        if ((viewport.MainCamera.Position - worldspace).Length() > 16)
            fontSpecification = GameManager.GameRenderer!.DefaultFontSpecification;

        Font font = GameManager.GameRenderer.FontRegistry.LoadFontFromSpecification(fontSpecification);
        Vector2 absscreenspace = Raylib.GetWorldToScreen(worldspace, viewport.MainCamera);
        Vector2 screenspace = absscreenspace;

        screenspace -= Raylib.MeasureTextEx(font, Parent.Name, fontSpecification.Size, 0) / 2;

        Rlgl.EnableDepthTest();

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
    private void RenderHealthbar(WorkspaceRendererViewport viewport)
    {
        BasePart? basePart = Parent!.FindFirstChild_Fast("Head") as BasePart;
        if (basePart == null)
            return;

        FontSpecification fontSpecification = GameManager.GameRenderer!.DefaultFontSpecification1_5em;
        Vector3 worldspace = basePart.Position + new Vector3(0, 1.25f, 0);
        Vector3 namedir = worldspace - viewport.MainCamera.Position;
        Vector3 targetdir = viewport.MainCamera.Target - viewport.MainCamera.Position;

        if (Vector3.Dot(namedir, targetdir) <= 0)
            return;

        Vector2 absscreenspace = Raylib.GetWorldToScreen(worldspace, viewport.MainCamera);

        float healthfraction = Health / MaxHealth;
        float healthbarscale = 1 / (viewport.MainCamera.Position - worldspace).Length();
        int healthbarwidth = 800;
        int healthbarheight = healthbarwidth / 20;

        Rlgl.EnableDepthTest();

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

/*

    Humanoid history, this used to be a built-in humanoid WASD controller

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
    if (Raylib.IsKeyPressed(KeyboardKey.Space))
    {
        Vector3 vector3 = model.PrimaryPart.Velocity;
        vector3.Y += JumpPower * 4;
        model.PrimaryPart.Velocity = vector3;
    }

*/