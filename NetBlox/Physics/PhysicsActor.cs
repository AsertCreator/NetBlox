using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using MoonSharp.Interpreter;
using NetBlox.Instances.Parts;
using NetBlox.Runtime.Bridges;

namespace NetBlox.Physics;

public class PhysicsActor
{
    public PhysicsSolver Owner;
    public RigidBody? RigidBody;
    public RigidBodyShape? Shape;

    public PhysicsActor(PhysicsSolver solver, BasePart reference)
    {
        Owner = solver;
        RigidBody = solver.LocalWorld.CreateRigidBody();
        RigidBody.Position = reference.Position;
        RigidBody.Orientation = reference.QuaternionRotation;
        RigidBody.Velocity = reference.stateLinearVelocity;
        RigidBody.AngularVelocity = reference.stateAngularVelocity;
        // RigidBody.EnableSpeculativeContacts = true;

        solver.PartMapFromRigidBodies[RigidBody] = reference.InstanceID;

        RigidBody.BeginCollide += HandleArbiter;
        
        if (!reference.InitializationSettings.IsForeign)
            RigidBody.MotionType = reference.Anchored ? MotionType.Static : MotionType.Dynamic;
        else
            RigidBody.MotionType = MotionType.Static;
    }

    private void HandleArbiter(Arbiter arbiter)
    {
        BasePart? part0;
        BasePart? part1;

        if (arbiter.Body1.Shapes.Count > 1 || arbiter.Body1.Shapes[0] is TransformedShape) // body 1 is a part assembly
        {
            PhysicsAssembly physicsAssembly = Owner.PhysicsAssemblyMap[arbiter.Body1];
            TransformedShape shape = physicsAssembly.ShapeMappingById[arbiter.Handle.Data.Key.Key1];
            part0 = physicsAssembly.ShapeMappingInverse[shape];
        }
        else
        {
            part0 = Owner.GameManager.GameRegistry.GetLocalInstanceById(Owner.PartMapFromRigidBodies[arbiter.Body1]) as BasePart;
        }

        if (arbiter.Body2.Shapes.Count > 1 || arbiter.Body2.Shapes[0] is TransformedShape) // body 2 is a part assembly
        {
            PhysicsAssembly physicsAssembly = Owner.PhysicsAssemblyMap[arbiter.Body2];
            TransformedShape shape = physicsAssembly.ShapeMappingById[arbiter.Handle.Data.Key.Key2];
            part1 = physicsAssembly.ShapeMappingInverse[shape];
        }
        else
        {
            part1 = Owner.GameManager.GameRegistry.GetLocalInstanceById(Owner.PartMapFromRigidBodies[arbiter.Body2]) as BasePart;
        }

        if (part0 != null && part1 != null && (part0.Touched.HasConnections || part0.Touched.HasWaiters))
            part0.Touched.Fire([ DynValue.NewUserData(InstanceBridge.PushUserData(Owner.GameManager, part1)) ]);

        if (part1 != null && part0 != null && (part1.Touched.HasConnections || part1.Touched.HasWaiters))
            part1.Touched.Fire([ DynValue.NewUserData(InstanceBridge.PushUserData(Owner.GameManager, part0)) ]);
    }

    public void SetMotionType(MotionType type)
    {
        if (RigidBody != null)
            RigidBody.MotionType = type;
    }
}