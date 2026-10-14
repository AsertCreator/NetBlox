using System.Diagnostics;
using Jitter2.Dynamics.Constraints;
using NetBlox.Runtime;
using Raylib_cs;

namespace NetBlox.Instances;

// synchronize all changes with Weld.cs

[Creatable]
public class Snap : JointInstance
{
    protected InstanceID lastpart0;
    protected InstanceID lastpart1;
    private WeldJoint? weldJoint;

    public override string ClassName => nameof(Snap);

    public Snap(ulong id, GameManager gameManager) : base(id, gameManager)
    {
        RegisterForEventId(GameEvent.EVENT_BEFORE_PHYSICS);
        RegisterForEventId(GameEvent.EVENT_RENDERGIZMOS);
    }

    private void CreateWeld()
    {
        Debug.Assert(Part0 != null);
        Debug.Assert(Part1 != null);
        Debug.Assert(Part0.PhysicsActor != null);
        Debug.Assert(Part1.PhysicsActor != null);
        Debug.Assert(Part0.PhysicsActor.RigidBody != null);
        Debug.Assert(Part1.PhysicsActor.RigidBody != null);

        if (GameManager.PhysicsSolver == null)
        {
            Trace.TraceWarning("Cannot apply weld when GameManager.PhysicsSolver is null!");
            return;
        }

        weldJoint = new WeldJoint(GameManager.PhysicsSolver.LocalWorld, Part0.PhysicsActor.RigidBody, Part1.PhysicsActor.RigidBody,
            Part1.statePosition);
        weldJoint.Enable();
    }
    private void DestroyWeld()
    {
        if (weldJoint != null)
        {
            weldJoint.Disable();
            weldJoint.Remove();
            weldJoint = null;
        }
    }

    public override void OnRegisteredEvent(EngineEventArgs args)
    {
        base.OnRegisteredEvent(args);

        if (args.GameEvent.Id == GameEvent.EVENT_RENDERGIZMOS)
        {
            if (Part0 != null && Part1 != null && weldJoint != null)
            {
                Rlgl.DisableDepthTest();
                Raylib.DrawCylinderEx(Part0.Position, Part1.Position, 0.13f, 0.13f, 3, new Color(60, 128, 60));
                Rlgl.EnableDepthTest();
            }
        }
        else if (args.GameEvent.Id == GameEvent.EVENT_BEFORE_PHYSICS)
        {
            if (weldJoint == null)
            {
                if (Part0 != null && Part1 != null && Part0.PhysicsActor != null && Part1.PhysicsActor != null)
                    CreateWeld();
            }
            else
            {
                if (Part0 == null || Part1 == null || Part0.PhysicsActor == null || Part1.PhysicsActor == null)
                    DestroyWeld();
            }
        }
    }

    public override void CommitStageDestroying()
    {
        base.CommitStageDestroying();
        DestroyWeld();
    }

    public override bool IsA(string className)
    {
        if (className != nameof(Snap))
            return base.IsA(className);
        return true;
    }
}