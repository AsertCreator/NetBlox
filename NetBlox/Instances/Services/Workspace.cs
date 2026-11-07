using NetBlox.Runtime;
using NetBlox.Structs;
using NetBlox.Instances.Parts;
using System.Numerics;
using System.Diagnostics;
using Jitter2.Collision;
using Jitter2.LinearMath;
using Jitter2.Collision.Shapes;

namespace NetBlox.Instances.Services;

[Service]
[ReplicateChildren]
[ExplorerIconIndex(19)]
public class Workspace : Model
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public float FallenPartsDestroyHeight { get; set; } = -50;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public AutoJointsMode AutoJointsMode { get; set; }

    public override string ClassName => nameof(Workspace);

    public const ulong NETWORK_CONSTANT_ID = 2;

    public Workspace(ulong id, GameManager gameManager) : base(NETWORK_CONSTANT_ID, gameManager)
    {
    }

    public TraceResult Trace(Vector3 from, Vector3 to, TraceCallback? traceCallback)
    {
        TraceResult traceResult = default;
        traceResult.Workspace = this;
        traceResult.Callback = traceCallback;
        traceResult.Start = from;
        traceResult.HitPart = null;
        traceResult.Fraction = 1;
        traceResult.Normal = default;
        traceResult.End = to;

        if (children.Count == 0)
            return traceResult;
        
        // TODO: custom tracing

        Debug.Assert(GameManager.PhysicsSolver != null);

        DynamicTree.RayCastFilterPost post = delegate(DynamicTree.RayCastResult result)
        {
            RigidBodyShape? rigidBodyShape = result.Entity as RigidBodyShape;
            if (rigidBodyShape == null)
                return false;
            BasePart? basePart = GameManager.PhysicsSolver.ResolvePartForShape(rigidBodyShape);
            if (basePart == null)
                return false;
            traceResult.Fraction = result.Lambda;
            traceResult.Normal = result.Normal;
            traceResult.HitPart = basePart;
            if (traceCallback != null)
                return traceCallback(basePart, ref traceResult);
            return true;
        };

        IDynamicTreeProxy? proxy = default;
        JVector normal = default;
        float lambda = default;
        bool hit = GameManager.PhysicsSolver.LocalWorld.DynamicTree.RayCast(from, to - from, 1, null, post,
            out proxy, out normal, out lambda);

        RigidBodyShape? rigidBodyShape = proxy as RigidBodyShape;
        if (rigidBodyShape != null)
        {
            BasePart? basePart = GameManager.PhysicsSolver.ResolvePartForShape(rigidBodyShape);
            
            traceResult.Fraction = lambda;
            traceResult.Normal = normal;
            traceResult.HitPart = basePart;

            return traceResult;
        }
        else
        {
            traceResult.Fraction = 1;
            traceResult.Normal = default;
            traceResult.HitPart = null;

            return traceResult;
        }
    }

    /// <summary>
    /// Makes joints between these parts. Horrifyingly slow, please never call this
    /// </summary>
    /// <param name="parts">The parts to be dealt with</param>
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.Plugin)]
    public void MakeJoints(BasePart[] parts)
    {
        for (int i = 0; i < parts.Length; i++)
        {
            GameManager.PhysicsSolver?.SpatialGrid.UpdatePart(parts[i]);

            BasePart[]? neighboring = GameManager.PhysicsSolver?.SpatialGrid.GetImmediatelyNeighboringParts(parts[i]);
            if (neighboring == null || neighboring.Length == 0)
                continue;

            parts[i].MakeJointsWith(neighboring.ToArray());
        }
    }

    public override void CommitStageAlive()
    {
        base.CommitStageAlive();

        if (AutoJointsMode == AutoJointsMode.Default || AutoJointsMode == AutoJointsMode.LegacyImplicit)
        {
            BasePart[] parts = GetPartDescendants();
            MakeJoints(parts);
        }
    }

    public override void Destroy()
    {
        if (!GameManager.GameScheduler.GetCurrentSecurityIdentity()!.RequireSimpleCapability(SimpleSecurityCapabilityLevel.DestroyServices))
            return;
        base.Destroy();
    }

    public override bool IsA(string className)
    {
        if (className != nameof(Workspace))
            return base.IsA(className);
        return true;
    }
}