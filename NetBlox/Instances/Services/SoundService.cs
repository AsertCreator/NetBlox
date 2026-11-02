using System.Diagnostics;
using System.Numerics;
using NetBlox.Rendering;
using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;

namespace NetBlox.Instances.Services;

[Service]
[ExplorerIconIndex(31)]
public class SoundService : Instance
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public bool EnableBirdAmbient { get; set; } = true;

    public override string ClassName => nameof(SoundService);

    private Music birdAmbientSound;
    private bool birdAmbientSoundLoaded;
    private bool lastEnableBirdAmbient = false;

    public const ulong NETWORK_CONSTANT_ID = 21;

    public SoundService(ulong id, GameManager gameManager) : base(NETWORK_CONSTANT_ID, gameManager)
    {
        
    }

    private SchedulerTaskResult MainAmbientSoundProcessor(GameSchedulerTask schedulerTask)
    {
        if (GameManager.ShuttingDown)
            return SchedulerTaskResult.CompletedSuccess;

        if (lastEnableBirdAmbient != EnableBirdAmbient)
        {
            if (birdAmbientSoundLoaded && !EnableBirdAmbient)
                Raylib.StopMusicStream(birdAmbientSound);
            else if (birdAmbientSoundLoaded && EnableBirdAmbient)
                Raylib.PlayMusicStream(birdAmbientSound);
            lastEnableBirdAmbient = EnableBirdAmbient;
        }

        if (EnableBirdAmbient)
        {
            float volume = 0;

            Raylib.UpdateMusicStream(birdAmbientSound);

            if (GameManager.PhysicsSolver == null)
            {
                volume = 1;
            }
            else
            {
                WorkspaceRendererViewport? workspaceRendererViewport = GameManager.GameRenderer!.CurrentViewport as WorkspaceRendererViewport;
                if (workspaceRendererViewport == null)
                {    
                    volume = 0;
                }
                else
                {
                    Workspace? workspace = Root.Workspace;
                    Vector3 vector3 = workspaceRendererViewport.MainCamera.Position;

                    volume = 0;

                    TraceResult? traceResult = workspace?.Trace(vector3, vector3 + Vector3.UnitY * 40, null);
                    if (traceResult.HasValue)
                        volume -= 0.5f * Math.Min(0, 1 - traceResult.Value.Fraction);
                    
                    traceResult = workspace?.Trace(vector3, vector3 - Vector3.UnitY * 40, null);
                    if (traceResult.HasValue)
                        volume += 1 - traceResult.Value.Fraction;
                }
            }

            volume = Math.Min(Math.Max(volume, 0), 1);

            Raylib.SetMusicVolume(birdAmbientSound, volume);
        }

        schedulerTask.WaitingTimeTarget = GameManager.TimestampInTheFuture(TimeSpan.FromMilliseconds(1000 / 30));
        
        return SchedulerTaskResult.NotCompleted;
    }

    public override void CommitStageInitialize()
    {
        base.CommitStageInitialize();

        if (GameManager.GameRenderer != null)
        {
            GameManager.GameAssetManager.QuickLoad("rbxasset://sounds/birdsambient.mp3")?
                .AddCallbackForFailure(x =>
                {
                    Trace.TraceWarning("Bird ambient sound missing - birds are dead");
                })
                .AddCallbackForSuccess(x =>
                {
                    birdAmbientSound = Raylib.LoadMusicStream(x.LocalDownloadPath);
                    birdAmbientSoundLoaded = true;
                });
            GameManager.GameScheduler.Schedule("Ambient Sound Processor", GameScheduler.SchedulerPhase.Rendering, MainAmbientSoundProcessor);
        }
    }

    public override void Destroy()
    {
        if (!GameManager.GameScheduler.GetCurrentSecurityIdentity()!.RequireSimpleCapability(SimpleSecurityCapabilityLevel.DestroyServices))
            return;
        base.Destroy();
    }

    public override bool IsA(string className)
    {
        if (className != nameof(SoundService))
            return base.IsA(className);
        return true;
    }
}