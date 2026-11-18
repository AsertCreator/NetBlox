using System.Diagnostics;
using System.Numerics;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using NetBlox.Instances;
using NetBlox.Instances.Parts;
using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;

namespace NetBlox.Physics;

public class PhysicsAssembly
{
    public readonly GameManager GameManager;
    public RigidBody CombinedRigidBody;
    public List<BasePart> Participants = [];
    public Dictionary<ulong, TransformedShape> ShapeMappingById = [];
    public Dictionary<BasePart, TransformedShape> ShapeMapping = [];
    public Dictionary<TransformedShape, BasePart> ShapeMappingInverse = [];
    public BasePart? CurrentAssemblyRoot;
    public BrickColor DebugColor = BrickColor.Random();

    public PhysicsAssembly(GameManager gameManager, BasePart first) : this(gameManager, first, true)
    {
    }

    private PhysicsAssembly(GameManager gameManager, BasePart first, bool findConnectedParts)
    {
        GameManager = gameManager;
        CurrentAssemblyRoot = first;

        Debug.Assert(first.PhysicsAssembly == null); // the first part to form a physics assembly cannot be in a different assembly, 
        // that operation is called joining.
        Debug.Assert(GameManager.PhysicsSolver != null);

        CombinedRigidBody = GameManager.PhysicsSolver.LocalWorld.CreateRigidBody();
        CombinedRigidBody.MotionType = MotionType.Dynamic;
        CombinedRigidBody.Velocity = first.stateLinearVelocity;
        CombinedRigidBody.AngularVelocity = first.stateAngularVelocity;
        // CombinedRigidBody.EnableSpeculativeContacts = true;

        GameManager.PhysicsSolver.PhysicsAssemblyMap[CombinedRigidBody] = this;

        /*
        if (first.OwnPhysicsActor != null)
        {
            if (first.OwnPhysicsActor.RigidBody != null)
            {
                GameManager.PhysicsSolver.LocalWorld.Remove(first.OwnPhysicsActor.RigidBody);
                first.OwnPhysicsActor.RigidBody = null;
            }

            first.OwnPhysicsActor = null;
            GameManager.PhysicsSolver.PartActors.Remove(first.InstanceID);
        }

        TransformedShape transformedShape = new TransformedShape(first.CreateShape(), new JVector());

        ShapeMapping[first] = transformedShape;
        ShapeMappingById[transformedShape.ShapeId] = transformedShape;
        ShapeMappingInverse[transformedShape] = first;
        CombinedRigidBody.AddShape(transformedShape);

        CombinedRigidBody.Position = first.Position;
        CombinedRigidBody.Orientation = first.QuaternionRotation;

        first.PhysicsAssembly = this;

        Participants.Add(first);
        */

        if (findConnectedParts)
            TryFastMassiveWeldPath(first);
    }

    private void TryFastMassiveWeldPath(BasePart from)
    {
        Debug.Assert(GameManager.PhysicsSolver != null);

        HashSet<JointInstance> foundJoints = new HashSet<JointInstance>();
        List<JointInstance> foundJoints_list = new List<JointInstance>();

        HashSet<BasePart> foundBaseParts = new HashSet<BasePart>();
        List<BasePart> foundBaseParts_list = new List<BasePart>();

        void WalkPart(BasePart? basePart)
        {
            if (basePart == null)
                return;
            if (foundBaseParts.Contains(basePart))
                return;
            foundBaseParts.Add(basePart);
            foundBaseParts_list.Add(basePart);

            foreach (InstanceID jointId in basePart.joints)
            {
                JointInstance? jointInstance = GameManager.GameRegistry.GetLocalInstanceById(jointId) as JointInstance;
                if (jointInstance == null)
                    continue;
                if (!jointInstance.IsA("Weld") && !jointInstance.IsA("Glue") && !jointInstance.IsA("Snap"))
                    continue;
                
                BasePart? otherPart = jointInstance.Part0;
                if (otherPart == basePart)
                    otherPart = jointInstance.Part1;

                if (otherPart == null)
                    continue;
                
                if (foundJoints.Add(jointInstance))
                    foundJoints_list.Add(jointInstance);

                WalkPart(otherPart);
            }
        }

        WalkPart(from);

        BasePart[] baseParts = foundBaseParts_list.ToArray();

        foreach (JointInstance joint in foundJoints_list)
        {
            if (joint is Weld weld)
                weld.SetWeldCreated(true);
            else if (joint is Glue glue)
                glue.SetWeldCreated(true);
            else if (joint is Snap snap)
                snap.SetWeldCreated(true);
        }

        CombinedRigidBody.Position = default;
        CombinedRigidBody.Orientation = Quaternion.Identity;

        AttachNewParts(baseParts.AsSpan());
    }

    public unsafe void AttachNewPart(BasePart basePart)
    {
        Debug.Assert(GameManager.PhysicsSolver != null);
        Debug.Assert(basePart.PhysicsAssembly == null);

        Participants.Add(basePart);

        if (basePart.OwnPhysicsActor != null)
        {
            if (basePart.OwnPhysicsActor.RigidBody != null)
            {
                GameManager.PhysicsSolver.LocalWorld.Remove(basePart.OwnPhysicsActor.RigidBody);
                basePart.OwnPhysicsActor.RigidBody = null;
            }

            basePart.OwnPhysicsActor = null;
            GameManager.PhysicsSolver.PartActors.Remove(basePart.InstanceID);
        }
        
        Vector3 differenceInPosition = Vector3.Transform(
            basePart.statePosition - CombinedRigidBody.Position,
            Quaternion.Inverse(CombinedRigidBody.Orientation));
        Quaternion localRotation = Quaternion.Inverse(basePart.stateRotation) * CombinedRigidBody.Orientation;
        Vector3 axisAngle = default;
        float angle = default;

        Raymath.QuaternionToAxisAngle(localRotation, &axisAngle, &angle);

        TransformedShape transformedShape = new TransformedShape(basePart.CreateShape(), 
            differenceInPosition, JMatrix.CreateRotationMatrix(axisAngle, angle));

        ShapeMapping[basePart] = transformedShape;
        ShapeMappingById[transformedShape.ShapeId] = transformedShape;
        ShapeMappingInverse[transformedShape] = basePart;
        CombinedRigidBody.AddShape(transformedShape, MassInertiaUpdateMode.Preserve);

        CombinedRigidBody.SetMassInertia();

        CenterOfMassToCenterOfGeometry();

        basePart.PhysicsAssembly = this;

        ReelectRoot();
    }
    public unsafe void AttachNewParts(Span<BasePart> baseParts)
    {
        Debug.Assert(GameManager.PhysicsSolver != null);

        Participants.AddRange(baseParts);

        for (int i = 0; i < baseParts.Length; i++)
        {
            BasePart basePart = baseParts[i];

            if (basePart.OwnPhysicsActor != null)
            {
                if (basePart.OwnPhysicsActor.RigidBody != null)
                {
                    GameManager.PhysicsSolver.LocalWorld.Remove(basePart.OwnPhysicsActor.RigidBody);
                    basePart.OwnPhysicsActor.RigidBody = null;
                }

                basePart.OwnPhysicsActor = null;
                GameManager.PhysicsSolver.PartActors.Remove(basePart.InstanceID);
            }

            Vector3 differenceInPosition = Vector3.Transform(
                basePart.statePosition - CombinedRigidBody.Position,
                Quaternion.Inverse(CombinedRigidBody.Orientation));
            Quaternion localRotation = Quaternion.Inverse(basePart.stateRotation) * CombinedRigidBody.Orientation;
            Vector3 axisAngle = default;
            float angle = default;

            Raymath.QuaternionToAxisAngle(localRotation, &axisAngle, &angle);

            TransformedShape transformedShape = new TransformedShape(basePart.CreateShape(), 
                differenceInPosition, JMatrix.CreateRotationMatrix(axisAngle, angle));

            ShapeMapping[basePart] = transformedShape;
            ShapeMappingById[transformedShape.ShapeId] = transformedShape;
            ShapeMappingInverse[transformedShape] = basePart;
            CombinedRigidBody.AddShape(transformedShape, MassInertiaUpdateMode.Preserve);

            basePart.PhysicsAssembly = this;
        }

        CombinedRigidBody.SetMassInertia();

        CenterOfMassToCenterOfGeometry();

        ReelectRoot();
    }
    public void RemovePart(BasePart basePart, bool finalizing = false)
    {
        if (ShapeMapping.TryGetValue(basePart, out TransformedShape? transformedShape))
        {
            CombinedRigidBody.RemoveShape(transformedShape);
            CombinedRigidBody.SetMassInertia();
            ShapeMapping.Remove(basePart);
            ShapeMappingById.Remove(transformedShape.ShapeId);
            ShapeMappingInverse.Remove(transformedShape);

            CenterOfMassToCenterOfGeometry();
        }

        Participants.Remove(basePart);
        basePart.PhysicsAssembly = null;
        if (!finalizing)
            GameManager.PhysicsSolver!.AddBasePart(basePart);

        RebuildDisconnectedComponents();
        ReelectRoot();
    }
    public void CenterOfMassToCenterOfGeometry()
    {
        Vector3 centerOfGeometrySum = default;
        Vector3 centerOfGeometry;

        foreach (RigidBodyShape shape in CombinedRigidBody.Shapes)
        {
            if (shape is TransformedShape anotherTransformedShape)
                centerOfGeometrySum += anotherTransformedShape.Translation;
        }

        centerOfGeometry = centerOfGeometrySum / CombinedRigidBody.Shapes.Count;

        if (float.IsNaN(centerOfGeometry.X) || float.IsNaN(centerOfGeometry.Y) || float.IsNaN(centerOfGeometry.Z))
            centerOfGeometry = default;

        foreach (RigidBodyShape shape in CombinedRigidBody.Shapes)
        {
            if (shape is TransformedShape anotherTransformedShape)
                anotherTransformedShape.Translation -= centerOfGeometry;
            GameManager.PhysicsSolver!.LocalWorld.DynamicTree.Update(shape);
        }

        CombinedRigidBody.Position += Vector3.Transform(centerOfGeometry, CombinedRigidBody.Orientation);

        CombinedRigidBody.ClearContactCache();
        CombinedRigidBody.SetMassInertia();
    }
    public unsafe void MergeWith(PhysicsAssembly physicsAssembly)
    {
        Debug.Assert(physicsAssembly != this);

        for (int i = 0; i < physicsAssembly.Participants.Count; i++)
        {
            physicsAssembly.Participants[i].PhysicsAssembly = this;
            physicsAssembly.Participants[i].OwnPhysicsActor = null;
            
            Vector3 differenceInPosition = Vector3.Transform(
                physicsAssembly.Participants[i].statePosition - CombinedRigidBody.Position,
                Quaternion.Inverse(CombinedRigidBody.Orientation));
            Quaternion localRotation = Quaternion.Inverse(physicsAssembly.Participants[i].stateRotation) * CombinedRigidBody.Orientation;
            Vector3 axisAngle = default;
            float angle = default;

            Raymath.QuaternionToAxisAngle(localRotation, &axisAngle, &angle);

            TransformedShape transformedShape = new TransformedShape(physicsAssembly.Participants[i].CreateShape(), 
                differenceInPosition, JMatrix.CreateRotationMatrix(axisAngle, angle));

            ShapeMapping[physicsAssembly.Participants[i]] = transformedShape;
            ShapeMappingById[transformedShape.ShapeId] = transformedShape;
            ShapeMappingInverse[transformedShape] = physicsAssembly.Participants[i];
            CombinedRigidBody.AddShape(transformedShape, MassInertiaUpdateMode.Preserve);

            Participants.Add(physicsAssembly.Participants[i]);
        }

        CombinedRigidBody.SetMassInertia();

        CenterOfMassToCenterOfGeometry();

        physicsAssembly.CombinedRigidBody.ClearShapes();
        physicsAssembly.Participants.Clear();
        physicsAssembly.ShapeMapping.Clear();
        physicsAssembly.ShapeMappingById.Clear();
        physicsAssembly.ShapeMappingInverse.Clear();
        physicsAssembly.ReelectRoot();
        GameManager.PhysicsSolver!.AllPhysicsAssemblies.Remove(physicsAssembly);
        GameManager.PhysicsSolver!.LocalWorld.Remove(physicsAssembly.CombinedRigidBody);

        ReelectRoot();
    }
    public void BreakBetween(BasePart a, BasePart b, JointInstance disconnectedJoint)
    {
        Debug.Assert(a.PhysicsAssembly != null);
        Debug.Assert(a.PhysicsAssembly == b.PhysicsAssembly);

        RebuildDisconnectedComponents(disconnectedJoint);
    }

    private void RebuildDisconnectedComponents(JointInstance? disconnectedJoint = null)
    {
        List<BasePart> activeParticipants = [];

        foreach (BasePart bp in Participants)
        {
            if (bp.InitializationStage < InitializationStage.Destroying)
                activeParticipants.Add(bp);
        }

        HashSet<BasePart> remaining = activeParticipants.ToHashSet();
        HashSet<BasePart> visited = [];
        List<List<BasePart>> components = [];

        foreach (BasePart part in activeParticipants)
        {
            if (!visited.Add(part))
                continue;

            List<BasePart> component = [];
            Stack<BasePart> pending = new();
            pending.Push(part);

            while (pending.TryPop(out BasePart? current))
            {
                component.Add(current);

                foreach (ulong jointId in current.joints)
                {
                    JointInstance? jointInstance = GameManager.GameRegistry.GetLocalInstanceById(jointId) as JointInstance;

                    if (jointInstance == null)
                        continue;
                    if (jointInstance == disconnectedJoint || !IsActiveJoint(jointInstance))
                        continue;

                    BasePart? other = jointInstance.Part0;
                    if (other == current)
                        other = jointInstance.Part1;

                    if (other != null && remaining.Contains(other) && visited.Add(other))
                        pending.Push(other);
                }
            }

            components.Add(component);
        }

        if (components.Count <= 1)
            return;

        foreach (BasePart participant in activeParticipants)
            participant.UpdateState();

        CombinedRigidBody.ClearContactCache();
        CombinedRigidBody.ClearShapes();

        foreach (BasePart participant in Participants)
            participant.PhysicsAssembly = null;

        Participants.Clear();
        ShapeMapping.Clear();
        ShapeMappingById.Clear();
        ShapeMappingInverse.Clear();
        CurrentAssemblyRoot = null;

        PhysicsSolver solver = GameManager.PhysicsSolver!;
        solver.AllPhysicsAssemblies.Remove(this);
        solver.PhysicsAssemblyMap.Remove(CombinedRigidBody);
        solver.LocalWorld.Remove(CombinedRigidBody);

        foreach (List<BasePart> component in components)
        {
            if (component.Count == 1)
            {
                solver.AddBasePart(component[0]);
            }
            else
            {
                PhysicsAssembly assembly = new PhysicsAssembly(GameManager, component[0], false);
                assembly.AttachNewParts(component.ToArray().AsSpan());
                solver.AllPhysicsAssemblies.Add(assembly);
            }
        }
    }
    private static bool IsActiveJoint(JointInstance joint)
    {
        if (joint is Snap snap)
            return snap.weldCreated;
        else if (joint is Glue glue)
            return glue.weldCreated;
        else if (joint is Weld weld)
            return weld.weldCreated;
        return false;
    }

    public void ReelectRoot()
    {
        if (Participants.Count == 0)
        {
            CurrentAssemblyRoot = null;
            return;
        }

        for (int i = 0; i < Participants.Count; i++)
        {
            if (Participants[i].Anchored)
            {
                CurrentAssemblyRoot = Participants[i];
                CombinedRigidBody.MotionType = MotionType.Static;
                return;
            }
        }

        CombinedRigidBody.MotionType = MotionType.Dynamic;

        BasePart? possibleRoot;

        possibleRoot = Participants.Find(x => x.Name == "HumanoidRootPart");
        if (possibleRoot != null)
        {
            CurrentAssemblyRoot = possibleRoot;
            return;
        }

        possibleRoot = Participants.Find(x => x.Name == "Torso");
        if (possibleRoot != null)
        {
            CurrentAssemblyRoot = possibleRoot;
            return;
        }

        CurrentAssemblyRoot = Participants[0];
    }
}