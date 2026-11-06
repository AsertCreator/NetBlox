using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Jitter2.Collision.Shapes;
using NetBlox.Instances.Services;
using NetBlox.Rendering;
using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;

namespace NetBlox.Instances.Parts;

[Creatable]
[ExplorerIconIndex(1)]
public class Part : BasePart
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]   
    public SurfaceType TopSurface { get; set; } = SurfaceType.Studs;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]   
    public SurfaceType LeftSurface { get; set; } = SurfaceType.Smooth;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]   
    public SurfaceType RightSurface { get; set; } = SurfaceType.Smooth;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]   
    public SurfaceType BottomSurface { get; set; } = SurfaceType.Smooth;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]   
    public SurfaceType FrontSurface { get; set; } = SurfaceType.Smooth;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]   
    public SurfaceType BackSurface { get; set; } = SurfaceType.Smooth;
    [LuaAlias(Name = "shape")]
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]   
    public PartType Shape 
    { 
        get => currentPartType;
        set
        {
            if (value == PartType.Cylinder)
                value = PartType.Block; // TODO: implement cylinders
            currentPartType = value;
        }
    }

    public override Vector3 Size 
    { 
        get => base.Size; 
        set
        {
            value.X = Math.Clamp(value.X, 0.001f, 2048);
            value.Y = Math.Clamp(value.Y, 0.001f, 2048);
            value.Z = Math.Clamp(value.Z, 0.001f, 2048);

            base.Size = value;
        }
    }
    public override string ClassName => nameof(Part);

    private string? lastDecalTextureLeft;
    private string? lastDecalTextureRight;
    private string? lastDecalTextureTop;
    private string? lastDecalTextureBottom;
    private string? lastDecalTextureFront;
    private string? lastDecalTextureBack;

    public GCTexture2D? lastDecalTextureLeftLoaded;
    public GCTexture2D? lastDecalTextureRightLoaded;
    public GCTexture2D? lastDecalTextureTopLoaded;
    public GCTexture2D? lastDecalTextureBottomLoaded;
    public GCTexture2D? lastDecalTextureFrontLoaded;
    public GCTexture2D? lastDecalTextureBackLoaded;
    
    // yes you can do that

    public WeakReference<Decal>? lastDecalTextureLeftInstance;
    public WeakReference<Decal>? lastDecalTextureRightInstance;
    public WeakReference<Decal>? lastDecalTextureTopInstance;
    public WeakReference<Decal>? lastDecalTextureBottomInstance;
    public WeakReference<Decal>? lastDecalTextureFrontInstance;
    public WeakReference<Decal>? lastDecalTextureBackInstance;

    private PartType currentPartType = PartType.Block;
    private PartType lastPartType = (PartType)999;

    public Part(ulong id, GameManager gameManager) : base(id, gameManager)
    {
        Size = new Vector3(4, 1, 2);

        GameManager.TryGetEventForId(GameEvent.EVENT_RENDERGIZMOS)?.RegisterInstance(this);
        GameManager.TryGetEventForId(GameEvent.EVENT_BEFORE_PHYSICS)?.RegisterInstance(this);
    }
    public override void CommitStageInitialize()
    {
        base.CommitStageInitialize();
    }

    public override void OnRegisteredEvent(EngineEventArgs args)
    {
        base.OnRegisteredEvent(args);

        if (ParentService is not Workspace)
            return;

        if (args.GameEvent.Id == GameEvent.EVENT_RENDER3D)
        {
            RenderingEventArgs? renderingEventArgs = args.EventData as RenderingEventArgs;

            if (renderingEventArgs == null)
                return;

            UpdateDecals();

            renderingEventArgs.WritePart(this);
        }
        else if (args.GameEvent.Id == GameEvent.EVENT_BEFORE_PHYSICS)
        {
            if (currentPartType != lastPartType && PhysicsAssembly == null && OwnPhysicsActor != null)
            {
                if (OwnPhysicsActor.RigidBody!.Shapes.Contains(OwnPhysicsActor.Shape))
                    OwnPhysicsActor.RigidBody.RemoveShape(OwnPhysicsActor.Shape!);
                else
                {
                    OwnPhysicsActor.Shape = CreateShape();
                    OwnPhysicsActor.RigidBody.AddShape(OwnPhysicsActor.Shape);
                }
                
                lastPartType = currentPartType;
            }
        }
        else if (args.GameEvent.Id == GameEvent.EVENT_RENDERGIZMOS)
        {
            if (PhysicsAssembly != null)
            {
                unsafe
                {
                    Rlgl.DisableDepthTest();
                    Rlgl.PushMatrix();
                    Vector3 v3;
                    float angle;
                    Raymath.QuaternionToAxisAngle(stateRotation, &v3, &angle);
                    Rlgl.Translatef(Position.X, Position.Y, Position.Z);
                    Rlgl.Rotatef(angle * 180 / MathF.PI, v3.X, v3.Y, v3.Z);
                    Rlgl.Scalef(Size.X, Size.Y, Size.Z);
                    Raylib.DrawCubeWires(default, 1, 1, 1, PhysicsAssembly.DebugColor.Color3);
                    Rlgl.PopMatrix();
                    Rlgl.EnableDepthTest();
                }
            }
        }
    }
    public override RigidBodyShape CreateShape()
    {
        switch (Shape)
        {
            case PartType.Block: 
                BoxShape boxShape = new BoxShape(Size);
                return boxShape;
            case PartType.Ball:
                float radius = Size.X;
                if (radius > Size.Y)
                    radius = Size.Y;
                if (radius > Size.Z)
                    radius = Size.Z;
                radius /= 2;

                SphereShape sphereShape = new SphereShape(radius);
                return sphereShape;
            default:
                throw new NotImplementedException("No cylinders for now");
        }
    }
    public void UpdateDecals()
    {
        using RentedSpan<Instance?> rentedSpan = GetChildren_Fast();

        Decal? currentDecalLeft = null;
        Decal? currentDecalRight = null;
        Decal? currentDecalTop = null;
        Decal? currentDecalBottom = null;
        Decal? currentDecalFront = null;
        Decal? currentDecalBack = null;

        for (int i = 0; i < rentedSpan.Values.Length; i++)
        {
            Decal? decal = rentedSpan.Values[i] as Decal;

            if (decal == null)
                continue;

            if (decal.Face == NormalId.Right)
                currentDecalRight = decal;
            else if (decal.Face == NormalId.Left)
                currentDecalLeft = decal;
            else if (decal.Face == NormalId.Top)
                currentDecalTop = decal;
            else if (decal.Face == NormalId.Bottom)
                currentDecalBottom = decal;
            else if (decal.Face == NormalId.Front)
                currentDecalFront = decal;
            else if (decal.Face == NormalId.Back)
                currentDecalBack = decal;
        }

        void HandleDecalFace(
            ref GCTexture2D? lastDecalTextureLoaded, ref string? lastDecalTexture, 
            ref WeakReference<Decal>? lastDecalTextureInstance, NormalId normalId, Decal currentDecal)
        {
            lastDecalTextureLoaded = null;

            AssetDownloadTask? assetDownloadTask = GameManager.GameAssetManager.QuickLoad(currentDecal.Content);
            if (assetDownloadTask == null)
            {    
                Trace.TraceWarning("Couldn't load decal!");
                lastDecalTexture = currentDecal.Content;
                lastDecalTextureInstance = new WeakReference<Decal>(currentDecal);
            }
            else
            {
                assetDownloadTask.AddCallbackForSuccess(x =>
                {
                    Texture2D texture2D = Raylib.LoadTexture(x.LocalDownloadPath);
                    GCTexture2D gCTexture2D = new GCTexture2D(texture2D);

                    switch (normalId)
                    {
                        case NormalId.Right:
                            lastDecalTextureRightLoaded = gCTexture2D;
                            break;
                        case NormalId.Left:
                            lastDecalTextureLeftLoaded = gCTexture2D;
                            break;
                        case NormalId.Top:
                            lastDecalTextureTopLoaded = gCTexture2D;
                            break;
                        case NormalId.Bottom:
                            lastDecalTextureBottomLoaded = gCTexture2D;
                            break;
                        case NormalId.Front:
                            lastDecalTextureFrontLoaded = gCTexture2D;
                            break;
                        case NormalId.Back:
                            lastDecalTextureBackLoaded = gCTexture2D;
                            break;
                    }
                });
                lastDecalTexture = currentDecal.Content;
                lastDecalTextureInstance = new WeakReference<Decal>(currentDecal);
            }
        }

        if (currentDecalRight == null)
            lastDecalTextureRightLoaded = null;
        else if (currentDecalRight.Content != lastDecalTextureRight)
            HandleDecalFace(ref lastDecalTextureRightLoaded, ref lastDecalTextureRight, ref lastDecalTextureRightInstance, NormalId.Right, currentDecalRight);

        if (currentDecalLeft == null)
            lastDecalTextureLeftLoaded = null;
        else if (currentDecalLeft.Content != lastDecalTextureLeft)
            HandleDecalFace(ref lastDecalTextureLeftLoaded, ref lastDecalTextureLeft, ref lastDecalTextureLeftInstance, NormalId.Left, currentDecalLeft);

        if (currentDecalTop == null)
            lastDecalTextureTopLoaded = null;
        else if (currentDecalTop.Content != lastDecalTextureTop)
            HandleDecalFace(ref lastDecalTextureTopLoaded, ref lastDecalTextureTop, ref lastDecalTextureTopInstance, NormalId.Top, currentDecalTop);

        if (currentDecalBottom == null)
            lastDecalTextureBottomLoaded = null;
        else if (currentDecalBottom.Content != lastDecalTextureBottom)
            HandleDecalFace(ref lastDecalTextureBottomLoaded, ref lastDecalTextureBottom, ref lastDecalTextureBottomInstance, NormalId.Bottom, currentDecalBottom);

        if (currentDecalFront == null)
            lastDecalTextureFrontLoaded = null;
        else if (currentDecalFront.Content != lastDecalTextureFront)
            HandleDecalFace(ref lastDecalTextureFrontLoaded, ref lastDecalTextureFront, ref lastDecalTextureFrontInstance, NormalId.Front, currentDecalFront);

        if (currentDecalBack == null)
            lastDecalTextureBackLoaded = null;
        else if (currentDecalBack.Content != lastDecalTextureBack)
            HandleDecalFace(ref lastDecalTextureBackLoaded, ref lastDecalTextureBack, ref lastDecalTextureBackInstance, NormalId.Back, currentDecalBack);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override void CalculateBoundingBox(out Vector3 min, out Vector3 max)
    {
        if (Shape != PartType.Ball)
            base.CalculateBoundingBox(out min, out max);
        else
        {
            min = Position - Size / 2;
            max = Position + Size / 2;
        }
    }

    public override bool IsA(string className)
    {
        if (className != nameof(Part))
            return base.IsA(className);
        return true;
    }
}