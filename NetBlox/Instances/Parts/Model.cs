using System.Numerics;
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