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

    public PhysicsAssembly(GameManager gameManager, BasePart first)
    {
        GameManager = gameManager;
        CurrentAssemblyRoot = first;

        Debug.Assert(first.PhysicsAssembly == null); // the first part to form a physics assembly cannot be in a different assembly, 
        // that operation is called joining.
        Debug.Assert(GameManager.PhysicsSolver != null);

        CombinedRigidBody = GameManager.PhysicsSolver.LocalWorld.CreateRigidBody();
        CombinedRigidBody.MotionType = MotionType.Dynamic;
        // CombinedRigidBody.EnableSpeculativeContacts = true;

        GameManager.PhysicsSolver.PhysicsAssemblyMap[CombinedRigidBody] = this;

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
        
        Vector3 differenceInPosition = Raymath.Vector3RotateByQuaternion(
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

            Vector3 differenceInPosition = Raymath.Vector3RotateByQuaternion(
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
        if (!finalizing)
            GameManager.PhysicsSolver!.AddBasePart(basePart);

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

        CombinedRigidBody.Position += Raymath.Vector3RotateByQuaternion(
            centerOfGeometry, CombinedRigidBody.Orientation);

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
            
            Vector3 differenceInPosition = Raymath.Vector3RotateByQuaternion(
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
    public void BreakBetween(BasePart a, BasePart b)
    {
        Debug.Assert(a.PhysicsAssembly != null);
        Debug.Assert(a.PhysicsAssembly == b.PhysicsAssembly);

        JointInstance? theweld = null;

        foreach (ulong jointId in a.joints)
        {
            Instance? instance = GameManager.GameRegistry.GetLocalInstanceById(jointId);
            if (instance == null)
                continue;
            if (instance.IsA("Weld") || instance.IsA("Snap") || instance.IsA("Glue"))
            {
                theweld = instance as JointInstance;
                break;
            }
        }

        if (theweld == null)
        {
            Trace.TraceWarning("Cannot break up the assembly between two parts that don't immediately connect with each other");
            return;
        }
        
        HashSet<ulong> allParts = [];
        HashSet<ulong> metParts = [];

        for (int i = 0; i < Participants.Count; i++)
            allParts.Add(Participants[i].InstanceID);

        void Walk(BasePart basePart, JointInstance? toIgnore)
        {
            metParts.Add(basePart.InstanceID);

            foreach (ulong jointId in basePart.joints)
            {
                JointInstance? currentweld = GameManager.GameRegistry.GetLocalInstanceById(jointId) as JointInstance;
                if (currentweld == null)
                    continue;
                if (!currentweld.IsA("Weld") && !currentweld.IsA("Snap") && !currentweld.IsA("Glue"))
                    continue;
                if (currentweld == toIgnore)
                    continue;
                
                BasePart? theOtherPart = currentweld.Part1;
                if (theOtherPart == basePart)
                    theOtherPart = currentweld.Part0;
                if (theOtherPart == basePart)
                    continue;

                if (theOtherPart == null)
                    continue;
                
                if (metParts.Contains(theOtherPart.InstanceID))
                    continue;
                
                Walk(theOtherPart, toIgnore);
            }
        }

        Walk(a, theweld);

        if (metParts.Count == allParts.Count)
        {
            // probably the assembly is still connected through other thingies
        }
        else
        {
            List<List<ulong>> newAssemblies = [];
            newAssemblies.Add(metParts.ToList());
            allParts.ExceptWith(metParts);
            metParts.Clear();

            while (allParts.Count > 0)
            {
                BasePart? basePart = GameManager.GameRegistry.GetLocalInstanceById(allParts.ElementAt(0)) as BasePart;
                for (int i = 1; basePart == null && i < allParts.Count; i++)
                    basePart = GameManager.GameRegistry.GetLocalInstanceById(allParts.ElementAt(i)) as BasePart;
                if (basePart == null)
                    break;
                Walk(basePart, theweld);

                newAssemblies.Add(metParts.ToList());
                allParts.ExceptWith(metParts);
                metParts.Clear();
            }

            // first we kill ourselves
            CurrentAssemblyRoot = null;
            CombinedRigidBody.ClearContactCache();
            CombinedRigidBody.ClearShapes();
            Participants.ForEach(x => x.PhysicsAssembly = null);
            Participants.Clear();
            // FlushDeadAssemblies will clean us up

            for (int i = 0; i < newAssemblies.Count; i++)
            {
                List<ulong> newAssembly = newAssemblies[i];
                BasePart[] newAssemblyParts = 
                    newAssembly.Select(x => GameManager.GameRegistry.GetLocalInstanceById(x) as BasePart)
                    .Where(x => x is not null).ToArray()!;

                if (newAssemblyParts.Length == 0)
                {
                    // uhh
                }
                else if (newAssemblyParts.Length == 1)
                {
                    GameManager.PhysicsSolver!.AddBasePart(newAssemblyParts[0]!);
                }
                else if (newAssemblyParts.Length == 2)
                {
                    newAssemblyParts[0].JoinAssembliesWith(newAssemblyParts[1]);
                }
                else
                {
                    newAssemblyParts[0].JoinAssembliesWith(newAssemblyParts[1]);
                    newAssemblyParts[0].PhysicsAssembly?.AttachNewParts(newAssemblyParts.AsSpan()[2..]);
                }
            }
        }

        if (theweld.InitializationStage < InitializationStage.Destroying)
            theweld.Destroy();
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