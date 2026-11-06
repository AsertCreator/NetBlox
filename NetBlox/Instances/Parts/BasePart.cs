using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;
using NetBlox.Physics;
using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;

namespace NetBlox.Instances.Parts;

public record struct SurfaceInfo(SurfaceType Type, Vector3 Center, Vector3 Normal, Vector3 TangentU, Vector3 TangentV, float HalfU, float HalfV);

public class BasePart : PVInstance
{
    public override string ClassName => nameof(BasePart);

    [LuaAlias(Name = "anchored")]
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual bool Anchored 
    { 
        get => stateAnchored;
        set
        {
            stateAnchored = value;
            if (InitializationStage >= InitializationStage.Initializing)
                GameManager.PhysicsSolver?.FinalizeSetAnchoredForBasePart(this, value);
        }
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual bool CastShadow { get; set; } = true;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual bool CanCollide { get; set; } = true;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual bool CanTouch { get; set; } = true;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual bool Locked { get; set; }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual float Transparency { get; set; }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual Vector3 Velocity 
    {
        get => stateLinearVelocity;
        set
        {
            stateLinearVelocity = value;
            if (InitializationStage >= InitializationStage.Initializing)
                GameManager.PhysicsSolver?.FinalizeSetLinearVelocityForBasePart(this, value);
        }
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual Vector3 Position 
    {
        get => statePosition;
        set
        {
            statePosition = value;
            if (InitializationStage >= InitializationStage.Initializing)
                GameManager.PhysicsSolver?.FinalizeSetPositionForBasePart(this, value);
        }
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual Vector3 AngularVelocity 
    {
        get => stateAngularVelocity;
        set
        {
            stateAngularVelocity = value;
            if (InitializationStage >= InitializationStage.Initializing)
                GameManager.PhysicsSolver?.FinalizeSetAngularVelocityForBasePart(this, value);
        }
    }[LuaAlias(Name = "size")]
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual Vector3 Size
    {
        get => stateSize;
        set
        {
            stateSize = value;
            if (InitializationStage >= InitializationStage.Initializing)
                GameManager.PhysicsSolver?.FinalizeSetSizeForBasePart(this, value);
        }
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual Vector3 Rotation
    {
        get
        {
            Vector3 euler = Raymath.QuaternionToEuler(QuaternionRotation);
            euler *= 180 / MathF.PI;
            return new Vector3(euler.Z, euler.Y, euler.X);
        }
        set
        {
            value /= 180 / MathF.PI;
            Quaternion quaternion = Raymath.QuaternionFromEuler(value.Z, value.Y, value.X);
            QuaternionRotation = quaternion;
        }
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual BrickColor BrickColor 
    { 
        get => brickColor;
        set
        {
            brickColor = value;
            color = brickColor.Color3;
        }
    }
    [LuaAlias(Name = "Color3uint8")]
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual Color3 Color3
    { 
        get => color;
        set
        {
            brickColor = BrickColor.GetBrickColorByClosestColor3(value);
            color = value;
        }
    }
    public virtual Quaternion QuaternionRotation
    {
        get => stateRotation;
        set
        {
            stateRotation = value;
            if (InitializationStage >= InitializationStage.Initializing)
                GameManager.PhysicsSolver?.FinalizeSetRotationForBasePart(this, value);
        }
    }
    [NotReplicated]
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual CoordinateFrame CFrame
    {
        get
        {
            CoordinateFrame cframe = default;
            cframe.Position = Position;
            cframe.Orientation = Raymath.QuaternionToMatrix(QuaternionRotation);
            return cframe;
        }
        set
        {
            Position = value.Position;
            QuaternionRotation = Raymath.QuaternionFromMatrix(value.Orientation);
        }
    }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public virtual LuaEvent Touched { get; } = new LuaEvent();

    public PhysicsActor? OwnPhysicsActor;
    public PhysicsAssembly? PhysicsAssembly;
    public HashSet<ulong> joints = [];

    public HashSet<BasePart> MakeJoints_Jointed = [];

    public bool stateAnchored;
    public Vector3 stateSize;
    public Vector3 statePosition;
    public Vector3 stateLinearVelocity;
    public Quaternion stateRotation = Quaternion.Identity;
    public Vector3 stateAngularVelocity;

    protected BrickColor brickColor;
    protected Color3 color;

    public BasePart(ulong id, GameManager gameManager) : base(id, gameManager)
    {
        GameManager.TryGetEventForId(GameEvent.EVENT_RENDER3D)?.RegisterInstance(this); 

        Size = new Vector3(1, 1, 1);
        BrickColor = BrickColor.MediumStoneGrey; // medium something gray
    }

    public override void CommitStageDestroying()
    {
        base.CommitStageDestroying();
        GameManager.PhysicsSolver?.FinalizeDestroyForBasePart(this);
    }
    public override void CommitStageInitialize()
    {
        base.CommitStageInitialize();
        if (PhysicsAssembly == null)
            GameManager.PhysicsSolver?.AddBasePart(this);
    }

    public bool UpdateState()
    {
        bool muchChanges = false;

        if (OwnPhysicsActor == null && PhysicsAssembly == null)
            return false;

        if (InitializationStage >= InitializationStage.Initializing)
        {
            float physicsError = 0.001f;

            if (OwnPhysicsActor != null && OwnPhysicsActor.RigidBody != null)
            {
                if (!statePosition.Length().WithinError(OwnPhysicsActor.RigidBody.Position.Length(), physicsError))
                    muchChanges = true;
                if (!stateRotation.Length().WithinError(OwnPhysicsActor.RigidBody.Orientation.Length(), physicsError))
                    muchChanges = true;
                if (!stateLinearVelocity.Length().WithinError(OwnPhysicsActor.RigidBody.Velocity.Length(), physicsError))
                    muchChanges = true;
                if (!stateAngularVelocity.Length().WithinError(OwnPhysicsActor.RigidBody.AngularVelocity.Length(), physicsError))
                    muchChanges = true;

                statePosition = OwnPhysicsActor.RigidBody.Position;
                stateRotation = OwnPhysicsActor.RigidBody.Orientation;
                stateLinearVelocity = OwnPhysicsActor.RigidBody.Velocity;
                stateAngularVelocity = OwnPhysicsActor.RigidBody.AngularVelocity;
            }
            if (PhysicsAssembly != null)
            {
                if (!PhysicsAssembly.ShapeMapping.TryGetValue(this, out TransformedShape? shape))
                    return muchChanges;

                Vector3 newposition = shape.WorldBoundingBox.Center;
                Vector3 newlinearvelocity = PhysicsAssembly.CombinedRigidBody.Velocity;
                Vector3 newangularvelocity = PhysicsAssembly.CombinedRigidBody.AngularVelocity;
                Quaternion newrotation = PhysicsAssembly.CombinedRigidBody.Orientation * JQuaternion.CreateFromMatrix(shape.Transformation);

                if (!statePosition.Length().WithinError(newposition.Length(), physicsError))
                    muchChanges = true;
                if (!stateRotation.Length().WithinError(newrotation.Length(), physicsError))
                    muchChanges = true;
                if (!stateLinearVelocity.Length().WithinError(newlinearvelocity.Length(), physicsError))
                    muchChanges = true;
                if (!stateAngularVelocity.Length().WithinError(newangularvelocity.Length(), physicsError))
                    muchChanges = true;

                statePosition = newposition;
                stateRotation = newrotation;
                stateLinearVelocity = newlinearvelocity;
                stateAngularVelocity = newangularvelocity;
            }
        }

        if (muchChanges)
        {
            GameManager.PhysicsSolver?.SpatialGrid.UpdatePart(this);
        }

        return muchChanges;
    }

    public void JoinAssembliesWith(BasePart other)
    {
        if (PhysicsAssembly == null && other.PhysicsAssembly != null)
            other.JoinAssembliesWith(this);
        else if (PhysicsAssembly == null && other.PhysicsAssembly == null)
        {
            PhysicsAssembly = new PhysicsAssembly(GameManager, this);
            OwnPhysicsActor = null;
            PhysicsAssembly.AttachNewPart(other);

            GameManager.PhysicsSolver!.AllPhysicsAssemblies.Add(PhysicsAssembly);
        }
        else if (PhysicsAssembly != null && other.PhysicsAssembly == null)
        {
            PhysicsAssembly.AttachNewPart(other);
        }
        else if (PhysicsAssembly != null && other.PhysicsAssembly != null)
        {
            if (PhysicsAssembly == other.PhysicsAssembly)
                return;
            PhysicsAssembly.MergeWith(other.PhysicsAssembly);
        }
    }
    public void BreakAssembliesWith(BasePart other)
    {
        if (PhysicsAssembly == null || PhysicsAssembly != other.PhysicsAssembly)
            return;

        PhysicsAssembly.BreakBetween(this, other);
    }
    public virtual RigidBodyShape CreateShape()
    {
        throw new NotImplementedException();
    }

    public virtual void MakeJointsWith(BasePart[] baseParts)
    {
        for (int i = 0; i < baseParts.Length; i++)
        {
            BasePart other = baseParts[i];
            if (other == this || MakeJoints_Jointed.Contains(other) || other.MakeJoints_Jointed.Contains(this))
                continue;

            if (other is Part firstPart && this is Part secondPart &&
                TryForConnection(firstPart, secondPart, SurfaceType.Studs, SurfaceType.Inlet))
            {
                Snap snap = GameManager.GameRegistry.Construct<Snap>();
                snap.Part0 = firstPart;
                snap.Part1 = secondPart;
                snap.Parent = firstPart;

                firstPart.MakeJoints_Jointed.Add(secondPart);
                secondPart.MakeJoints_Jointed.Add(firstPart);
            }
        }
    }

    //  i took it from the webs still slow as

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static bool TryForConnection(Part firstPart, Part secondPart, SurfaceType surfaceTypeFirst, SurfaceType surfaceTypeSecond)
    {
        for (int i = 0; i < firstPart.GameManager.PhysicsSolver!.JointSurfaceAxes.Length; i++)
        {
            SurfaceInfo surface = GetJointSurface(firstPart, i);

            for (int j = 0; j < firstPart.GameManager.PhysicsSolver!.JointSurfaceAxes.Length; j++)
            {
                SurfaceInfo secondSurface = GetJointSurface(secondPart, j);
                bool surfacesMatch = (surface.Type == surfaceTypeFirst && secondSurface.Type == surfaceTypeSecond) || (surface.Type == surfaceTypeSecond && secondSurface.Type == surfaceTypeFirst);

                if (!surfacesMatch || !AreJointSurfacesTouching(surface, secondSurface, firstPart.GameManager.PhysicsSolver!.JointSurfaceTolerance))
                    continue;

                return true;
            }
        }

        return false;
    }
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static SurfaceInfo GetJointSurface(Part part, int faceIndex)
    {
        (int normalAxis, float normalSign, int tangentUAxis, int tangentVAxis) = part.GameManager.PhysicsSolver!.JointSurfaceAxes[faceIndex];
        Vector3 normal = Vector3.Transform(GetAxis(normalAxis) * normalSign, part.QuaternionRotation);
        Vector3 tangentU = Vector3.Transform(GetAxis(tangentUAxis), part.QuaternionRotation);
        Vector3 tangentV = Vector3.Transform(GetAxis(tangentVAxis), part.QuaternionRotation);
        Vector3 size = part.Size;

        SurfaceType surfaceType = faceIndex switch
        {
            0 => part.TopSurface, 1 => part.BottomSurface,
            2 => part.LeftSurface, 3 => part.RightSurface,
            4 => part.FrontSurface, 5 => part.BackSurface,
            _ => throw new ArgumentOutOfRangeException(nameof(faceIndex))
        };

        return new SurfaceInfo(surfaceType,
            part.Position + normal * (GetAxisComponent(size, normalAxis) / 2),
            normal, tangentU, tangentV,
            GetAxisComponent(size, tangentUAxis) / 2,
            GetAxisComponent(size, tangentVAxis) / 2);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static bool AreJointSurfacesTouching(SurfaceInfo surface, SurfaceInfo secondSurface, float tolerance)
    {
        if (Vector3.Dot(surface.Normal, secondSurface.Normal) > -0.9999f)
            return false;

        Vector3 centerDelta = secondSurface.Center - surface.Center;
        if (MathF.Abs(Vector3.Dot(centerDelta, surface.Normal)) > tolerance)
            return false;
        
        return OverlapsAlongAxis(tolerance, centerDelta, surface.TangentU, surface.TangentU, surface.TangentV, surface.HalfU, surface.HalfV,
                   secondSurface.TangentU, secondSurface.TangentV, secondSurface.HalfU, secondSurface.HalfV) &&
               OverlapsAlongAxis(tolerance, centerDelta, surface.TangentV, surface.TangentU, surface.TangentV, surface.HalfU, surface.HalfV,
                   secondSurface.TangentU, secondSurface.TangentV, secondSurface.HalfU, secondSurface.HalfV) &&
               OverlapsAlongAxis(tolerance, centerDelta, secondSurface.TangentU, surface.TangentU, surface.TangentV, surface.HalfU, surface.HalfV,
                   secondSurface.TangentU, secondSurface.TangentV, secondSurface.HalfU, secondSurface.HalfV) &&
               OverlapsAlongAxis(tolerance, centerDelta, secondSurface.TangentV, surface.TangentU, surface.TangentV, surface.HalfU, surface.HalfV,
                   secondSurface.TangentU, secondSurface.TangentV, secondSurface.HalfU, secondSurface.HalfV);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool OverlapsAlongAxis(float tolerance, Vector3 centerDelta, Vector3 axis,
        Vector3 firstTangentU, Vector3 firstTangentV, float firstHalfU, float firstHalfV,
        Vector3 secondTangentU, Vector3 secondTangentV, float secondHalfU, float secondHalfV)
    {
        float distance = MathF.Abs(Vector3.Dot(centerDelta, axis));
        float firstRadius = firstHalfU * MathF.Abs(Vector3.Dot(firstTangentU, axis)) + firstHalfV * MathF.Abs(Vector3.Dot(firstTangentV, axis));
        float secondRadius = secondHalfU * MathF.Abs(Vector3.Dot(secondTangentU, axis)) + secondHalfV * MathF.Abs(Vector3.Dot(secondTangentV, axis));

        return distance <= firstRadius + secondRadius + tolerance || distance <= firstRadius + secondRadius - tolerance;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3 GetAxis(int axis) => axis switch
    {
        0 => Vector3.UnitX, 1 => Vector3.UnitY, 2 => Vector3.UnitZ,
        _ => throw new ArgumentOutOfRangeException(nameof(axis))
    };
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float GetAxisComponent(Vector3 vector, int axis) => axis switch
    {
        0 => vector.X, 1 => vector.Y, 2 => vector.Z,
        _ => throw new ArgumentOutOfRangeException(nameof(axis))
    };

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public virtual void CalculateBoundingBox(out Vector3 min, out Vector3 max)
    {
        if (QuaternionRotation != Quaternion.Identity)
        {
            Vector3 v3_0 = new Vector3(Size.X, Size.Y, Size.Z) / 2;
            Vector3 v3_1 = new Vector3(Size.X, Size.Y, -Size.Z) / 2;
            Vector3 v3_2 = new Vector3(Size.X, -Size.Y, Size.Z) / 2;
            Vector3 v3_3 = new Vector3(Size.X, -Size.Y, -Size.Z) / 2;
            Vector3 v3_4 = new Vector3(-Size.X, Size.Y, Size.Z) / 2;
            Vector3 v3_5 = new Vector3(-Size.X, Size.Y, -Size.Z) / 2;
            Vector3 v3_6 = new Vector3(-Size.X, -Size.Y, Size.Z) / 2;
            Vector3 v3_7 = new Vector3(-Size.X, -Size.Y, -Size.Z) / 2;

            v3_0 = Vector3.Transform(v3_0, QuaternionRotation);
            v3_1 = Vector3.Transform(v3_1, QuaternionRotation);
            v3_2 = Vector3.Transform(v3_2, QuaternionRotation);
            v3_3 = Vector3.Transform(v3_3, QuaternionRotation);
            v3_4 = Vector3.Transform(v3_4, QuaternionRotation);
            v3_5 = Vector3.Transform(v3_5, QuaternionRotation);
            v3_6 = Vector3.Transform(v3_6, QuaternionRotation);
            v3_7 = Vector3.Transform(v3_7, QuaternionRotation);

            // i'm so sorry

            Vector3 tempmin = new Vector3(
                MathF.Min(MathF.Min(MathF.Min(MathF.Min(MathF.Min(MathF.Min(MathF.Min(v3_0.X, v3_1.X), v3_2.X), v3_3.X), v3_4.X), v3_5.X), v3_6.X), v3_7.X),
                MathF.Min(MathF.Min(MathF.Min(MathF.Min(MathF.Min(MathF.Min(MathF.Min(v3_0.Y, v3_1.Y), v3_2.Y), v3_3.Y), v3_4.Y), v3_5.Y), v3_6.Y), v3_7.Y),
                MathF.Min(MathF.Min(MathF.Min(MathF.Min(MathF.Min(MathF.Min(MathF.Min(v3_0.Z, v3_1.Z), v3_2.Z), v3_3.Z), v3_4.Z), v3_5.Z), v3_6.Z), v3_7.Z)
            );
            Vector3 tempmax = new Vector3(
                MathF.Max(MathF.Max(MathF.Max(MathF.Max(MathF.Max(MathF.Max(MathF.Max(v3_0.X, v3_1.X), v3_2.X), v3_3.X), v3_4.X), v3_5.X), v3_6.X), v3_7.X),
                MathF.Max(MathF.Max(MathF.Max(MathF.Max(MathF.Max(MathF.Max(MathF.Max(v3_0.Y, v3_1.Y), v3_2.Y), v3_3.Y), v3_4.Y), v3_5.Y), v3_6.Y), v3_7.Y),
                MathF.Max(MathF.Max(MathF.Max(MathF.Max(MathF.Max(MathF.Max(MathF.Max(v3_0.Z, v3_1.Z), v3_2.Z), v3_3.Z), v3_4.Z), v3_5.Z), v3_6.Z), v3_7.Z)
            );

            min = tempmin + Position;
            max = tempmax + Position;
        }
        else
        {
            min = Position - Size / 2;
            max = Position + Size / 2;
        }
    }

    public override bool IsA(string className)
    {
        if (className != nameof(BasePart))
            return base.IsA(className);
        return true;
    }
}