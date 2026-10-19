using System.Diagnostics;
using System.Numerics;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using NetBlox.Instances.Parts;
using NetBlox.Structs;
using Raylib_cs;

namespace NetBlox.Physics;

public class PhysicsAssembly
{
    public readonly GameManager GameManager;
    public RigidBody CombinedRigidBody;
    public List<BasePart> Participants = [];
    public Dictionary<BasePart, TransformedShape> ShapeMapping = [];
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

        if (first.OwnPhysicsActor != null)
        {
            if (first.OwnPhysicsActor.RigidBody != null)
            {
                if (first.OwnPhysicsActor.Shape != null)
                {
                    TransformedShape transformedShape = new TransformedShape(first.OwnPhysicsActor.Shape, new JVector());

                    ShapeMapping[first] = transformedShape;
                    CombinedRigidBody.AddShape(transformedShape);
                }
                
                GameManager.PhysicsSolver.LocalWorld.Remove(first.OwnPhysicsActor.RigidBody);
                first.OwnPhysicsActor.RigidBody = null;
            }

            first.OwnPhysicsActor = null;
        }
        else
        {
            TransformedShape transformedShape = new TransformedShape(first.CreateShape(), new JVector());

            ShapeMapping[first] = transformedShape;
            CombinedRigidBody.AddShape(transformedShape);
        }

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
                if (basePart.OwnPhysicsActor.Shape != null)
                {
                    Vector3 differenceInPosition = Raymath.Vector3RotateByQuaternion(
                        basePart.statePosition - CombinedRigidBody.Position,
                        Quaternion.Inverse(CombinedRigidBody.Orientation));
                    Quaternion localRotation = Quaternion.Inverse(basePart.stateRotation) * CombinedRigidBody.Orientation;
                    Vector3 axisAngle = default;
                    float angle = default;

                    Raymath.QuaternionToAxisAngle(localRotation, &axisAngle, &angle);

                    TransformedShape transformedShape = new TransformedShape(basePart.OwnPhysicsActor.Shape, 
                        differenceInPosition, JMatrix.CreateRotationMatrix(axisAngle, angle));

                    ShapeMapping[basePart] = transformedShape;
                    CombinedRigidBody.AddShape(transformedShape, MassInertiaUpdateMode.Preserve);
                }
                
                GameManager.PhysicsSolver.LocalWorld.Remove(basePart.OwnPhysicsActor.RigidBody);
                basePart.OwnPhysicsActor.RigidBody = null;
            }

            basePart.OwnPhysicsActor = null;
            GameManager.PhysicsSolver.PartActors.Remove(basePart.InstanceID);
        }
        else
        {
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
            CombinedRigidBody.AddShape(transformedShape, MassInertiaUpdateMode.Preserve);
        }

        CombinedRigidBody.SetMassInertia();

        CenterOfMassToCenterOfGeometry();

        basePart.PhysicsAssembly = this;

        ReelectRoot();
    }
    public void RemovePart(BasePart basePart)
    {
        if (ShapeMapping.TryGetValue(basePart, out TransformedShape? transformedShape))
        {
            CombinedRigidBody.RemoveShape(transformedShape);
            CombinedRigidBody.SetMassInertia();
            ShapeMapping.Remove(basePart);

            CenterOfMassToCenterOfGeometry();
        }

        Participants.Remove(basePart);
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
            CombinedRigidBody.AddShape(transformedShape, MassInertiaUpdateMode.Preserve);

            Participants.Add(physicsAssembly.Participants[i]);
        }

        CombinedRigidBody.SetMassInertia();

        CenterOfMassToCenterOfGeometry();

        physicsAssembly.CombinedRigidBody.ClearShapes();
        physicsAssembly.Participants.Clear();
        physicsAssembly.ShapeMapping.Clear();
        physicsAssembly.ReelectRoot();
        GameManager.PhysicsSolver!.AllPhysicsAssemblies.Remove(physicsAssembly);
        GameManager.PhysicsSolver!.LocalWorld.Remove(physicsAssembly.CombinedRigidBody);

        ReelectRoot();
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
                return;
            }
        }

        CurrentAssemblyRoot = Participants[0];
    }
}