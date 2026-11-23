using System.Numerics;
using NetBlox.Instances.Parts;
using NetBlox.Rendering;
using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;

namespace NetBlox.Instances;

[Creatable]
[ExplorerIconIndex(5)]
public class Camera : Instance
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public CoordinateFrame CFrame { get; set; } = new CoordinateFrame()
    {
        Orientation = Matrix4x4.Identity,
        Position = default
    };
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public float FieldOfView { get; set; } = 90;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public Instance? CameraSubject { get; set; }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public CameraType CameraType { get; set; } = CameraType.Follow;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public Vector2 ViewportSize
    {
        get => new Vector2(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
    }

    public override string ClassName => nameof(Camera);

    public Camera(ulong id, GameManager gameManager) : base(id, gameManager)
    {
        RegisterForEventId(GameEvent.EVENT_BEFORE_RENDER);
    }

    public override void OnRegisteredEvent(EngineEventArgs args)
    {
        base.OnRegisteredEvent(args);

        if (args.GameEvent.Id == GameEvent.EVENT_BEFORE_RENDER)
        {
            if (GameManager.GameRenderer == null)
                return;

            CameraHandlingEventArgs? cameraHandlingEventArgs = args.EventData as CameraHandlingEventArgs;
            if (cameraHandlingEventArgs == null)
                return;
            if (GameManager.NetworkMode == Network.NetworkMode.Server)
                return;
            
            WorkspaceRendererViewport? workspaceRendererViewport = GameManager.GameRenderer.CurrentViewport as WorkspaceRendererViewport;
            if (workspaceRendererViewport == null)
                return;

            cameraHandlingEventArgs.CameraHandled = true;
            HandleCamera(workspaceRendererViewport);
        }
    }

    private void HandleCamera(WorkspaceRendererViewport workspaceRendererViewport)
    {
        workspaceRendererViewport.MainCamera.Up = Vector3.UnitY;
        workspaceRendererViewport.MainCamera.FovY = FieldOfView;

        if (CameraType == CameraType.Fixed || CameraType == CameraType.Scriptable || CameraType == CameraType.Custom)
        {
            Vector3 targetDifference = Vector3.Transform(Vector3.UnitX, CFrame.Orientation);

            workspaceRendererViewport.MainCamera.Position = CFrame.Position;
            workspaceRendererViewport.MainCamera.Target = CFrame.Position + targetDifference;
        }
        else if (CameraType == CameraType.Follow)
        {
            Vector3 positionTargetDifference = workspaceRendererViewport.MainCamera.Position - workspaceRendererViewport.MainCamera.Target;
            Vector3 subjectPosition = workspaceRendererViewport.MainCamera.Target;
            
            if (CameraSubject is null)
                return;
            else if (CameraSubject is BasePart basePart)
                subjectPosition = basePart.Position;
            else if (CameraSubject is Humanoid humanoid)
                subjectPosition = humanoid.GetHeadPosition() ?? subjectPosition;
        
            workspaceRendererViewport.MainCamera.Position = subjectPosition + positionTargetDifference;
            workspaceRendererViewport.MainCamera.Target = subjectPosition;
        }
    }

    public override bool IsA(string className)
    {
        if (className != nameof(Camera))
            return base.IsA(className);
        return true;
    }
}