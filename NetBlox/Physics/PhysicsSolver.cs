using System.Numerics;
using System.Runtime.CompilerServices;
using Jitter2;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using NetBlox.Instances.Parts;
using NetBlox.Instances.Services;
using NetBlox.Network;
using NetBlox.Runtime;

namespace NetBlox.Physics;

public class PhysicsSolver
{
    public World LocalWorld;
    public GameManager GameManager;
    public bool CanRun = false;
    public bool EnableNetworkPrediction = true;
    public int SendPhysicsEveryNFrames = 1;

    private int spcounter;

    public List<PhysicsAssembly> AllPhysicsAssemblies = [];
    public Dictionary<RigidBody, ulong> PartMapFromRigidBodies = new Dictionary<RigidBody, ulong>();
    public Dictionary<ulong, PhysicsActor> PartActors = new Dictionary<ulong, PhysicsActor>();
    public Dictionary<RigidBody, PhysicsAssembly> PhysicsAssemblyMap = new Dictionary<RigidBody, PhysicsAssembly>();

    public PhysicsSolver(GameManager gameManager)
    {
        GameManager = gameManager;
        LocalWorld = new World();
        LocalWorld.Gravity = new JVector(0, -18f, 0);
        LocalWorld.DynamicTree.Filter = DynamicTreeFilter;
    }

    private bool DynamicTreeFilter(IDynamicTreeProxy proxyA, IDynamicTreeProxy proxyB)
    {
        RigidBodyShape? shapeA = proxyA as RigidBodyShape;
        RigidBodyShape? shapeB = proxyB as RigidBodyShape;

        if (shapeA == null || shapeB == null)
            return true;

        if (shapeA.RigidBody == shapeB.RigidBody)
            return false;

        BasePart? basePartA = ResolvePartForShape(shapeA);
        BasePart? basePartB = ResolvePartForShape(shapeB);

        if (basePartA == null || basePartB == null)
            return true;

        if (basePartA.CanCollide && basePartB.CanCollide)
            return true;

        return false;
    }

    public BasePart? ResolvePartForShape(RigidBodyShape shape)
    {
        if (shape is TransformedShape transformedShape) // part of an assembly
        {
            if (!PhysicsAssemblyMap.TryGetValue(transformedShape.RigidBody, out PhysicsAssembly? assembly))
                return null;
            return assembly.ShapeMappingInverse[transformedShape];
        }
        else
        {
            return GameManager.GameRegistry.GetLocalInstanceById(PartMapFromRigidBodies[shape.RigidBody]) as BasePart;
        }
    }

    public void Step()
    {
        if (!CanRun)
            return;

        int fps = 60;
        if (GameManager.GameRenderer != null)
            fps = GameManager.GameRenderer.PreferredFPS;

        GameManager.TryGetEventForId(GameEvent.EVENT_BEFORE_PHYSICS)?.Fire();

        lock (this)
            LocalWorld.Step(1 / (float)fps, false);

        Workspace workspace = GameManager.RootModel.GetService<Workspace>();
        NetworkServer? networkServer = null;
        if (GameManager.NetworkMode == NetworkMode.Server)
            networkServer = GameManager.RootModel.GetService<NetworkServer>();

        bool sendPhysics = false;
        if (++spcounter == SendPhysicsEveryNFrames)
        {
            sendPhysics = true;
            spcounter = 0;
        }

        foreach (KeyValuePair<ulong, PhysicsActor> kvp in PartActors)
        {
            BasePart? basePart = GameManager.GameRegistry.GetLocalInstanceById(kvp.Key) as BasePart;
            if (basePart == null)
                continue;
            if (basePart.PhysicsAssembly != null)
                continue;

            if (!basePart.InitializationSettings.IsForeign)
            {
                if (!basePart.Anchored)
                {
                    bool muchChanges = basePart.UpdateState();
                    if (networkServer != null && muchChanges && sendPhysics)
                        networkServer.AddBroadcastPhysicsUpdate(basePart);
                }
            }
            else if (EnableNetworkPrediction)
            {
                if (!basePart.Anchored)
                {
                    basePart.statePosition += basePart.Velocity / fps;
                }
            }

            if (!basePart.Anchored && basePart.statePosition.Y < workspace.FallenPartsDestroyHeight)
                basePart.Destroy();
        }

        using RentedSpan<PhysicsAssembly> immutable = new RentedSpan<PhysicsAssembly>(AllPhysicsAssemblies.Count);
        AllPhysicsAssemblies.CopyTo(immutable.Values);

        foreach (PhysicsAssembly physicsAssembly in immutable.Values)
        {
            using RentedSpan<BasePart> expireds = new RentedSpan<BasePart>(physicsAssembly.Participants.Count);
            int expiredcount = 0;

            foreach (BasePart basePart in physicsAssembly.Participants)
            {
                if (!basePart.InitializationSettings.IsForeign)
                {
                    if (!basePart.Anchored)
                    {
                        bool muchChanges = basePart.UpdateState();
                        if (networkServer != null && muchChanges && sendPhysics)
                            networkServer.AddBroadcastPhysicsUpdate(basePart);
                    }
                }

                if (!basePart.Anchored && basePart.statePosition.Y < workspace.FallenPartsDestroyHeight)
                    expireds.Values[expiredcount++] = basePart;
            }

            for (int i = 0; i < expiredcount; i++)
                expireds.Values[i].Destroy();
        }

        FlushDeadAssemblies();

        if (sendPhysics)
            networkServer?.CommitBroadcastPhysicsUpdates();

        GameManager.TryGetEventForId(GameEvent.EVENT_AFTER_PHYSICS)?.Fire();
    }

    public void FlushDeadAssemblies()
    {
        using RentedSpan<PhysicsAssembly> deadOnes = new RentedSpan<PhysicsAssembly>(AllPhysicsAssemblies.Count);
        int deadOnesCount = 0;

        for (int i = 0; i < AllPhysicsAssemblies.Count; i++)
        {
            if (AllPhysicsAssemblies[i].Participants.Count == 0)
                deadOnes.Values[deadOnesCount++] = AllPhysicsAssemblies[i];
        }

        for (int i = 0; i < deadOnesCount; i++)
        {
            PhysicsAssembly assembly = deadOnes.Values[i];
            LocalWorld.Remove(assembly.CombinedRigidBody);
            AllPhysicsAssemblies.Remove(deadOnes.Values[i]);
            PhysicsAssemblyMap.Remove(assembly.CombinedRigidBody);
        }
    }

    public void FinalizeSetAnchoredForBasePart(BasePart part, bool anchored)
    {
        PhysicsActor? actor = part.OwnPhysicsActor;
        if (actor != null)
        {
            if (part.InitializationSettings.IsForeign)
            {
                actor.SetMotionType(MotionType.Static);
            }
            else
            {
                if (anchored)
                    actor.SetMotionType(MotionType.Static);
                else
                    actor.SetMotionType(MotionType.Dynamic);
            }
        }

        PhysicsAssembly? assembly = part.PhysicsAssembly;
        if (assembly != null)
        {
            bool hasAnchoredParticipant = false;
            for (int i = 0; i < assembly.Participants.Count; i++)
            {
                if (assembly.Participants[i].Anchored)
                {
                    hasAnchoredParticipant = true;
                    break;
                }
            }

            assembly.CombinedRigidBody.MotionType = hasAnchoredParticipant
                ? MotionType.Static
                : MotionType.Dynamic;

            assembly.ReelectRoot();
        }
    }
    public void FinalizeSetSizeForBasePart(BasePart part, Vector3 value)
    {
        RigidBodyShape? foundShape = null;
        PhysicsActor? actor = part.OwnPhysicsActor;
        PhysicsAssembly? assembly = part.PhysicsAssembly;
        RigidBody? rigidBody = null;

        if (actor != null)
        {
            foundShape = actor.Shape;
            rigidBody = actor.RigidBody;
        }
        if (assembly != null)
        {
            assembly.ShapeMapping.TryGetValue(part, out TransformedShape? transformedShape);
            rigidBody = assembly.CombinedRigidBody;

            foundShape = transformedShape?.OriginalShape;
        }

        if (foundShape == null || rigidBody == null)
            return;

        if (foundShape is BoxShape boxShape)
        {
            boxShape.Size = value;
        }
        else if (foundShape is SphereShape sphereShape)
        {
            float radius = value.X;
            if (radius > value.Y)
                radius = value.Y;
            if (radius > value.Z)
                radius = value.Z;
            radius /= 2;

            sphereShape.Radius = radius;
        }

        LocalWorld.DynamicTree.Update(foundShape);
        rigidBody.ClearContactCache();
        rigidBody.SetMassInertia();
    }
    public void FinalizeSetPositionForBasePart(BasePart part, Vector3 value)
    {
        PhysicsActor? actor = part.OwnPhysicsActor;
        if (actor != null)
        {
            if (actor.RigidBody == null)
                return;
            actor.RigidBody.Position = value;
        }

        PhysicsAssembly? assembly = part.PhysicsAssembly;
        if (assembly != null)
        {
            if (!assembly.ShapeMapping.TryGetValue(part, out TransformedShape? shape))
                return;
            Vector3 difference = value - assembly.CombinedRigidBody.Position;
            shape.Translation = difference;

            LocalWorld.DynamicTree.Update(shape);
            assembly.CombinedRigidBody.ClearContactCache();
            assembly.CombinedRigidBody.SetMassInertia();
        }
    }
    public void FinalizeSetRotationForBasePart(BasePart part, Quaternion value)
    {
        PhysicsActor? actor = part.OwnPhysicsActor;
        if (actor != null)
        {
            if (actor.RigidBody == null)
                return;
            actor.RigidBody.Orientation = value;
        }

        PhysicsAssembly? assembly = part.PhysicsAssembly;
        if (assembly != null)
        {
            if (!assembly.ShapeMapping.TryGetValue(part, out TransformedShape? shape))
                return;
            Quaternion difference = Quaternion.Inverse(value) * assembly.CombinedRigidBody.Orientation;
            shape.Transformation = JMatrix.CreateFromQuaternion(difference);

            LocalWorld.DynamicTree.Update(shape);
            assembly.CombinedRigidBody.ClearContactCache();
            assembly.CombinedRigidBody.SetMassInertia();
        }
    }
    public void FinalizeSetLinearVelocityForBasePart(BasePart part, Vector3 value)
    {
        PhysicsActor? actor = part.OwnPhysicsActor;
        if (actor != null)
        {
            if (actor.RigidBody == null)
                return;
            if (actor.RigidBody.MotionType == MotionType.Dynamic)
                actor.RigidBody.Velocity = value;
        }

        PhysicsAssembly? assembly = part.PhysicsAssembly;
        if (assembly != null)
        {
            if (!assembly.ShapeMapping.TryGetValue(part, out TransformedShape? shape))
                return;

            Vector3 velocity = assembly.CombinedRigidBody.Velocity;
            velocity -= velocity / assembly.Participants.Count;
            velocity += value * (1 / (float)assembly.Participants.Count);
            assembly.CombinedRigidBody.Velocity = velocity;
        }
    }
    public void FinalizeSetAngularVelocityForBasePart(BasePart part, Vector3 value)
    {
        PhysicsActor? actor = part.OwnPhysicsActor;
        if (actor != null)
        {
            if (actor.RigidBody == null)
                return;
            if (actor.RigidBody.MotionType == MotionType.Dynamic)
                actor.RigidBody.AngularVelocity = value;
        }

        PhysicsAssembly? assembly = part.PhysicsAssembly;
        if (assembly != null)
        {
            if (!assembly.ShapeMapping.TryGetValue(part, out TransformedShape? shape))
                return;

            Vector3 velocity = assembly.CombinedRigidBody.AngularVelocity;
            velocity -= velocity / assembly.Participants.Count;
            velocity += value * (1 / (float)assembly.Participants.Count);
            assembly.CombinedRigidBody.AngularVelocity = velocity;
        }
    }
    public void FinalizeDestroyForBasePart(BasePart part)
    {
        PhysicsActor? actor = part.OwnPhysicsActor;
        if (actor != null)
        {
            if (actor.RigidBody != null)
                LocalWorld.Remove(actor.RigidBody);
            PartActors.Remove(part.InstanceID);
            PartMapFromRigidBodies.Remove(actor.RigidBody!);
        }

        PhysicsAssembly? assembly = part.PhysicsAssembly;
        if (assembly != null)
        {
            assembly.RemovePart(part, true);
        }

        part.OwnPhysicsActor = null;
    }

    public void AddBasePart(BasePart basePart)
    {
        if (basePart.InitializationStage >= InitializationStage.Destroying)
            return;
        PhysicsActor physicsActor = new PhysicsActor(this, basePart);
        PartActors[basePart.InstanceID] = physicsActor;
        basePart.OwnPhysicsActor = physicsActor;
    }
    public PhysicsActor? GetPhysicsActor(BasePart basePart)
    {
        PartActors.TryGetValue(basePart.InstanceID, out PhysicsActor? actor);
        return actor;
    }
    public int GetActorCount()
    {
        return PartActors.Count;
    }
}