using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using NetBlox.Instances.Parts;

namespace NetBlox.Physics;

public enum PhysicsActorType
{
    BasePart, Humanoid
}
public class PhysicsActor
{
    public PhysicsSolver Owner;
    public PhysicsActorType Type;
    public RigidBody? RigidBody;
    public BoxShape? BoxShape;

    public PhysicsActor(PhysicsSolver solver, PhysicsActorType type, BasePart reference)
    {
        Owner = solver;
        Type = type;
        BoxShape = new BoxShape(reference.Size);
        RigidBody = solver.LocalWorld.CreateRigidBody();
        RigidBody.AddShape(BoxShape);
        RigidBody.Position = reference.Position;
        RigidBody.Orientation = reference.QuaternionRotation;
        
        if (!reference.InitializationSettings.IsForeign)
            RigidBody.MotionType = reference.Anchored ? MotionType.Static : MotionType.Dynamic;
        else
            RigidBody.MotionType = MotionType.Static;
    }

    public void SetMotionType(MotionType type)
    {
        if (RigidBody != null)
            RigidBody.MotionType = type;
    }
}