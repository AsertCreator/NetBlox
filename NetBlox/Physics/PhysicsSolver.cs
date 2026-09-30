using System.Numerics;
using Jitter2;
using Jitter2.Dynamics;
using NetBlox.Instances.Parts;
using NetBlox.Network;

namespace NetBlox.Physics;

public class PhysicsSolver
{
    public World LocalWorld;
    public GameManager GameManager;
    public bool CanRun = false;
    public bool EnableNetworkPrediction = true;
    public int SendPhysicsEveryNFrames = 1;

    private int spcounter;

    private Dictionary<ulong, PhysicsActor> partActors = new Dictionary<ulong, PhysicsActor>();

    public PhysicsSolver(GameManager gameManager)
    {
        GameManager = gameManager;
        LocalWorld = new World();
        LocalWorld.Gravity = new Jitter2.LinearMath.JVector(0, -18f, 0);
    }

    public void Step()
    {
        if (!CanRun)
            return;

        int fps = 60;
        if (GameManager.GameRenderer != null)
            fps = GameManager.GameRenderer.PreferredFPS;

        lock (this)
            LocalWorld.Step(1 / (float)fps, false);

        NetworkServer? networkServer = null;
        if (GameManager.NetworkMode == NetworkMode.Server)
            networkServer = GameManager.RootModel.GetService<NetworkServer>();

        bool sendPhysics = false;
        if (++spcounter == SendPhysicsEveryNFrames)
        {
            sendPhysics = true;
            spcounter = 0;
        }

        foreach (KeyValuePair<ulong, PhysicsActor> kvp in partActors)
        {
            BasePart? basePart = GameManager.GameRegistry.GetLocalInstanceById(kvp.Key) as BasePart;
            if (basePart == null)
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
        }

        if (sendPhysics)
            networkServer?.CommitBroadcastPhysicsUpdates();
    }

    public void FinalizeSetAnchoredForBasePart(BasePart part, bool anchored)
    {
        PhysicsActor? actor = GetPhysicsActor(part);
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
    }
    public void FinalizeSetSizeForBasePart(BasePart part, Vector3 value)
    {
        PhysicsActor? actor = GetPhysicsActor(part);
        if (actor != null)
        {
            if (actor.BoxShape == null)
                return;
            actor.BoxShape.Size = value;
        }
    }
    public void FinalizeSetPositionForBasePart(BasePart part, Vector3 value)
    {
        PhysicsActor? actor = GetPhysicsActor(part);
        if (actor != null)
        {
            if (actor.RigidBody == null)
                return;
            actor.RigidBody.Position = value;
        }
    }
    public void FinalizeSetRotationForBasePart(BasePart part, Quaternion value)
    {
        PhysicsActor? actor = GetPhysicsActor(part);
        if (actor != null)
        {
            if (actor.RigidBody == null)
                return;
            actor.RigidBody.Orientation = value;
        }
    }
    public void FinalizeSetLinearVelocityForBasePart(BasePart part, Vector3 value)
    {
        PhysicsActor? actor = GetPhysicsActor(part);
        if (actor != null)
        {
            if (actor.RigidBody == null)
                return;
            if (actor.RigidBody.MotionType == MotionType.Dynamic)
                actor.RigidBody.Velocity = value;
        }
    }
    public void FinalizeSetAngularVelocityForBasePart(BasePart part, Vector3 value)
    {
        PhysicsActor? actor = GetPhysicsActor(part);
        if (actor != null)
        {
            if (actor.RigidBody == null)
                return;
            if (actor.RigidBody.MotionType == MotionType.Dynamic)
                actor.RigidBody.AngularVelocity = value;
        }
    }
    public void FinalizeDestroyForBasePart(BasePart part)
    {
        PhysicsActor? actor = GetPhysicsActor(part);
        if (actor != null)
        {
            if (actor.RigidBody != null)
                LocalWorld.Remove(actor.RigidBody);
            partActors.Remove(part.InstanceID);
        }
    }

    public void AddBasePart(BasePart basePart)
    {
        PhysicsActor physicsActor = new PhysicsActor(this, PhysicsActorType.BasePart, basePart);
        partActors[basePart.InstanceID] = physicsActor;
        basePart.PhysicsActor = physicsActor;
    }
    public PhysicsActor? GetPhysicsActor(BasePart basePart)
    {
        partActors.TryGetValue(basePart.InstanceID, out PhysicsActor? actor);
        return actor;
    }
    public int GetActorCount()
    {
        return partActors.Count;
    }
}