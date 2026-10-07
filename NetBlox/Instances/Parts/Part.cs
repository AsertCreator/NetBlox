using System.Numerics;
using Jitter2.Collision.Shapes;
using NetBlox.Rendering;
using NetBlox.Runtime;
using NetBlox.Structs;

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
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]   
    public PartType Shape 
    { 
        get => currentPartType;
        set => currentPartType = value;
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

        GameManager.TryGetEventForId(GameEvent.EVENT_BEFORE_PHYSICS)?.RegisterInstance(this);
    }
    public override void CommitStageInitialize()
    {
        base.CommitStageInitialize();
    }

    public override unsafe void OnRegisteredEvent(EngineEventArgs args)
    {
        base.OnRegisteredEvent(args);

        if (args.GameEvent.Id == GameEvent.EVENT_RENDER3D)
        {
            RenderingEventArgs renderingEventArgs = (RenderingEventArgs)args.EventData!;

            renderingEventArgs.WritePart(this);
        }
        else if (args.GameEvent.Id == GameEvent.EVENT_BEFORE_PHYSICS)
        {
            if (currentPartType != lastPartType)
            {
                if (PhysicsActor == null || PhysicsActor.RigidBody == null)
                    return;
                RigidBodyShape? rigidBodyShape = PhysicsActor.Shape as RigidBodyShape;
                if (rigidBodyShape != null)
                    PhysicsActor.RigidBody.RemoveShape(rigidBodyShape);
                
                switch (currentPartType)
                {
                    case PartType.Block: 
                        BoxShape boxShape = new BoxShape(Size);
                        PhysicsActor.Shape = boxShape;
                        PhysicsActor.RigidBody.AddShape(boxShape);
                        break;
                    case PartType.Ball:
                        float radius = Size.X;
                        if (radius > Size.Y)
                            radius = Size.Y;
                        if (radius > Size.Z)
                            radius = Size.Z;
                        radius /= 2;

                        SphereShape sphereShape = new SphereShape(radius);
                        PhysicsActor.Shape = sphereShape;
                        PhysicsActor.RigidBody.AddShape(sphereShape);
                        break;
                    case PartType.Cylinder:
                        throw new NotImplementedException("No cylinders for now");
                }

                lastPartType = currentPartType;
            }
        }
    }

    public override bool IsA(string className)
    {
        if (className != nameof(Part))
            return base.IsA(className);
        return true;
    }
}