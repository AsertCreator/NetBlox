using System.Numerics;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;

namespace NetBlox.Instances.Parts;

[Creatable]
public class Model : PVInstance
{
    public BasePart? PrimaryPart
    {
        get => GameManager.GameRegistry.GetLocalInstanceById(primaryPart) as BasePart;
        set => primaryPart = value != null ? value.InstanceID : 0;
    }

    private InstanceID primaryPart;
    private BasePart[]? partDescendants;

    public override string ClassName => nameof(Model);

    public Model(ulong id, GameManager gameManager) : base(id, gameManager)
    {
        RegisterForEventId(GameEvent.EVENT_BEFORE_PHYSICS);

        DescendantAdded.OnBeforeFired += (_, _) =>
        {
            partDescendants = null;
        };
        DescendantRemoved.OnBeforeFired += (_, _) =>
        {
            partDescendants = null;
        };
    }

    public BasePart[] GetPartDescendants()
    {
        if (partDescendants != null)
            return partDescendants;
        Instance[] instances = GetDescendants();
        List<BasePart> baseParts = new List<BasePart>();
        for (int i = 0; i < instances.Length; i++)
        {
            if (instances[i] is BasePart basePart)
                baseParts.Add(basePart);
        }
        partDescendants = baseParts.ToArray();
        return partDescendants;
    }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public void TranslateBy(Vector3 delta)
    {
        // how much in o() is this

        BasePart[] instances = GetPartDescendants();
        for (int i = 0; i < instances.Length; i++)
            instances[i].Position += delta;
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public void PivotTo(CoordinateFrame coordinateFrame)
    {
        // o(n^tree(3))

        BasePart? primary = PrimaryPart;
        BasePart[] instances = GetPartDescendants();

        if (primary == null)
            primary = FindFirstChild_Fast("HumanoidRootPart") as BasePart;
        if (primary == null)
            primary = instances[0];
        if (primary == null)
            return;

        Quaternion cfOrientation = Raymath.QuaternionFromMatrix(coordinateFrame.Orientation);
        Quaternion orientationDifference = Quaternion.Inverse(primary.QuaternionRotation) * cfOrientation;
        Vector3 ogPrimaryPosition = primary.statePosition;

        for (int i = 0; i < instances.Length; i++)
        {
            Vector3 localPositionDifference = instances[i].statePosition - ogPrimaryPosition;
            localPositionDifference = Raymath.Vector3RotateByQuaternion(localPositionDifference, orientationDifference);
            instances[i].Position = coordinateFrame.Position + localPositionDifference;
            instances[i].QuaternionRotation *= orientationDifference;
        }
    }
    // wow how helpful of you vscode copilot
    public Vector3 TransformDisplacementForCollisionalSliding(Vector3 displacement)
    {
        BasePart? primary = PrimaryPart;
        Workspace workspace = Root.GetService<Workspace>();
        BasePart[] instances = GetPartDescendants();

        if (primary == null)
            primary = FindFirstChild_Fast("HumanoidRootPart") as BasePart;
        if (primary == null)
            primary = instances[0];
        if (primary == null)
            return displacement;

        float halfX = MathF.Max(0.01f, primary.Size.X / 2 - 0.02f);
        float halfY = MathF.Max(0.01f, primary.Size.Y / 2 - 0.02f);
        float halfZ = MathF.Max(0.01f, primary.Size.Z / 2 - 0.02f);

        Vector3[] offsets = [
            new Vector3(0, -halfY, 0), default, new Vector3(0, halfY, 0),
            new Vector3(-halfX, -halfY, -halfZ), new Vector3(-halfX, 0, -halfZ), new Vector3(-halfX, halfY, -halfZ),
            new Vector3(-halfX, -halfY, halfZ), new Vector3(-halfX, 0, halfZ), new Vector3(-halfX, halfY, halfZ),
            new Vector3(halfX, -halfY, -halfZ), new Vector3(halfX, 0, -halfZ), new Vector3(halfX, halfY, -halfZ),
            new Vector3(halfX, -halfY, halfZ), new Vector3(halfX, 0, halfZ), new Vector3(halfX, halfY, halfZ)
        ];

        Vector3 result = Vector3.Zero;
        Vector3 remaining = displacement;

        for (int slide = 0; slide < 3 && remaining.LengthSquared() > 0.00001f; slide++)
        {
            float nearestFraction = 1f;
            Vector3 hitNormal = default;
            Vector3 origin = primary.Position + result;

            foreach (Vector3 offset in offsets)
            {
                TraceCallback traceCallback = delegate (BasePart? part, ref TraceResult _)
                {
                    return part != null && !part.IsDescendantOf(this) && part.CanCollide;
                };

                TraceResult hitResults = workspace.Trace(origin + offset, origin + offset + remaining, traceCallback);

                if (hitResults.HitPart != null && hitResults.Fraction < nearestFraction)
                {
                    nearestFraction = hitResults.Fraction;
                    hitNormal = hitResults.Normal;
                }
            }

            if (nearestFraction >= 1f)
            {
                result += remaining;
                break;
            }

            float distance = remaining.Length();
            float safeFraction = MathF.Max(0f, nearestFraction - 0.02f / distance);

            Vector3 step = remaining * safeFraction;
            result += step;
            remaining -= step;

            hitNormal.Y = 0;
            if (hitNormal.LengthSquared() < 0.00001f)
                break;

            hitNormal = Vector3.Normalize(hitNormal);
            if (Vector3.Dot(hitNormal, remaining) > 0)
                hitNormal = -hitNormal;

            float intoWall = Vector3.Dot(remaining, hitNormal);
            if (intoWall >= 0)
                break;

            remaining -= hitNormal * intoWall;
        }

        return result;
    }

    public override void OnBeforePhysics()
    {
        base.OnBeforePhysics();
        if (PrimaryPart != null && !PrimaryPart.IsDescendantOf(this))
            PrimaryPart = null;
    }

    public override bool IsA(string className)
    {
        if (className != nameof(Model))
            return base.IsA(className);
        return true;
    }
}