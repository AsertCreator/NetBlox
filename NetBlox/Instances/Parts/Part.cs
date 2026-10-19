using System.Numerics;
using Jitter2.Collision.Shapes;
using NetBlox.Instances.Services;
using NetBlox.Rendering;
using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;

namespace NetBlox.Instances.Parts;

[Creatable]
public class Part : BasePart
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]   
    public SurfaceType TopSurface { get; set; } = SurfaceType.Studs;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]   
    public SurfaceType LeftSurface { get; set; } = SurfaceType.Smooth;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]   
    public SurfaceType RightSurface { get; set; } = SurfaceType.Smooth;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]   
    public SurfaceType BottomSurface { get; set; } = SurfaceType.Smooth;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]   
    public SurfaceType FrontSurface { get; set; } = SurfaceType.Smooth;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]   
    public SurfaceType BackSurface { get; set; } = SurfaceType.Smooth;
    [LuaAlias(Name = "shape")]
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]   
    public PartType Shape 
    { 
        get => currentPartType;
        set
        {
            if (value == PartType.Cylinder)
                value = PartType.Block; // TODO: implement cylinders
            currentPartType = value;
        }
    }

    public override Vector3 Size 
    { 
        get => base.Size; 
        set
        {
            value.X = Math.Clamp(value.X, 0.001f, 2048);
            value.Y = Math.Clamp(value.Y, 0.001f, 2048);
            value.Z = Math.Clamp(value.Z, 0.001f, 2048);

            base.Size = value;
        }
    }
    public override string ClassName => nameof(Part);

    private PartType currentPartType = PartType.Block;
    private PartType lastPartType = (PartType)999;

    public Part(ulong id, GameManager gameManager) : base(id, gameManager)
    {
        Size = new Vector3(4, 1, 2);

        GameManager.TryGetEventForId(GameEvent.EVENT_RENDERGIZMOS)?.RegisterInstance(this);
        GameManager.TryGetEventForId(GameEvent.EVENT_BEFORE_PHYSICS)?.RegisterInstance(this);
    }
    public override void CommitStageInitialize()
    {
        base.CommitStageInitialize();
    }

    public override void OnRegisteredEvent(EngineEventArgs args)
    {
        base.OnRegisteredEvent(args);

        if (ParentService is not Workspace)
            return;

        if (args.GameEvent.Id == GameEvent.EVENT_RENDER3D)
        {
            RenderingEventArgs renderingEventArgs = (RenderingEventArgs)args.EventData!;

            renderingEventArgs.WritePart(this);
        }
        else if (args.GameEvent.Id == GameEvent.EVENT_BEFORE_PHYSICS)
        {
            if (currentPartType != lastPartType && PhysicsAssembly == null && OwnPhysicsActor != null)
            {
                if (OwnPhysicsActor.RigidBody!.Shapes.Contains(OwnPhysicsActor.Shape))
                    OwnPhysicsActor.RigidBody.RemoveShape(OwnPhysicsActor.Shape!);
                else
                {
                    OwnPhysicsActor.Shape = CreateShape();
                    OwnPhysicsActor.RigidBody.AddShape(OwnPhysicsActor.Shape);
                }
                
                lastPartType = currentPartType;
            }
        }
        else if (args.GameEvent.Id == GameEvent.EVENT_RENDERGIZMOS)
        {
            if (PhysicsAssembly != null)
            {
                unsafe
                {
                    Rlgl.DisableDepthTest();
                    Rlgl.PushMatrix();
                    Vector3 v3;
                    float angle;
                    Raymath.QuaternionToAxisAngle(stateRotation, &v3, &angle);
                    Rlgl.Translatef(Position.X, Position.Y, Position.Z);
                    Rlgl.Rotatef(angle * 180 / MathF.PI, v3.X, v3.Y, v3.Z);
                    Rlgl.Scalef(Size.X, Size.Y, Size.Z);
                    Raylib.DrawCubeWires(default, 1, 1, 1, PhysicsAssembly.DebugColor.Color3);
                    Rlgl.PopMatrix();
                    Rlgl.EnableDepthTest();
                }
            }
        }
    }
    public override RigidBodyShape CreateShape()
    {
        switch (Shape)
        {
            case PartType.Block: 
                BoxShape boxShape = new BoxShape(Size);
                return boxShape;
            case PartType.Ball:
                float radius = Size.X;
                if (radius > Size.Y)
                    radius = Size.Y;
                if (radius > Size.Z)
                    radius = Size.Z;
                radius /= 2;

                SphereShape sphereShape = new SphereShape(radius);
                return sphereShape;
            default:
                throw new NotImplementedException("No cylinders for now");
        }
    }

    public override bool IsA(string className)
    {
        if (className != nameof(Part))
            return base.IsA(className);
        return true;
    }
}