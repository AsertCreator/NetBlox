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
                if (statePosition.Length().WithinError(OwnPhysicsActor.RigidBody.Position.Length(), physicsError))
                    muchChanges = true;
                if (stateRotation.Length().WithinError(OwnPhysicsActor.RigidBody.Orientation.Length(), physicsError))
                    muchChanges = true;
                if (stateLinearVelocity.Length().WithinError(OwnPhysicsActor.RigidBody.Velocity.Length(), physicsError))
                    muchChanges = true;
                if (stateAngularVelocity.Length().WithinError(OwnPhysicsActor.RigidBody.AngularVelocity.Length(), physicsError))
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

                if (statePosition.Length().WithinError(newposition.Length(), physicsError))
                    muchChanges = true;
                if (stateRotation.Length().WithinError(newrotation.Length(), physicsError))
                    muchChanges = true;
                if (stateLinearVelocity.Length().WithinError(newlinearvelocity.Length(), physicsError))
                    muchChanges = true;
                if (stateAngularVelocity.Length().WithinError(newangularvelocity.Length(), physicsError))
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            v3_0 = Raymath.Vector3RotateByQuaternion(v3_0, QuaternionRotation);
            v3_1 = Raymath.Vector3RotateByQuaternion(v3_1, QuaternionRotation);
            v3_2 = Raymath.Vector3RotateByQuaternion(v3_2, QuaternionRotation);
            v3_3 = Raymath.Vector3RotateByQuaternion(v3_3, QuaternionRotation);
            v3_4 = Raymath.Vector3RotateByQuaternion(v3_4, QuaternionRotation);
            v3_5 = Raymath.Vector3RotateByQuaternion(v3_5, QuaternionRotation);
            v3_6 = Raymath.Vector3RotateByQuaternion(v3_6, QuaternionRotation);
            v3_7 = Raymath.Vector3RotateByQuaternion(v3_7, QuaternionRotation);

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