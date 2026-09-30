using System.Numerics;
using NetBlox.Physics;
using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;

namespace NetBlox.Instances.Parts;

public class BasePart : PVInstance
{
    public override string ClassName => nameof(BasePart);

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
    }
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

    public PhysicsActor? PhysicsActor;

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
        GameManager.PhysicsSolver?.AddBasePart(this);
    }

    public bool UpdateState()
    {
        bool muchChanges = false;

        if (InitializationStage >= InitializationStage.Initializing)
        {
            if (PhysicsActor!.RigidBody != null)
            {
                float physicsError = 0.001f;

                if (statePosition.Length().WithinError(PhysicsActor!.RigidBody.Position.Length(), physicsError))
                    muchChanges = true;
                if (stateRotation.Length().WithinError(PhysicsActor!.RigidBody.Orientation.Length(), physicsError))
                    muchChanges = true;
                if (stateLinearVelocity.Length().WithinError(PhysicsActor!.RigidBody.Velocity.Length(), physicsError))
                    muchChanges = true;
                if (stateAngularVelocity.Length().WithinError(PhysicsActor!.RigidBody.AngularVelocity.Length(), physicsError))
                    muchChanges = true;

                statePosition = PhysicsActor!.RigidBody.Position;
                stateRotation = PhysicsActor!.RigidBody.Orientation;
                stateLinearVelocity = PhysicsActor!.RigidBody.Velocity;
                stateAngularVelocity = PhysicsActor!.RigidBody.AngularVelocity;
            }
        }

        return muchChanges;
    }

    public override bool IsA(string className)
    {
        if (className != nameof(BasePart))
            return base.IsA(className);
        return true;
    }
}