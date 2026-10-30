using NetBlox.Runtime;

namespace NetBlox.Instances.Services;

[Service]
[ReplicateChildren]
[ExplorerIconIndex(79)]
public class StarterPlayer : Instance
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public bool AutoJumpEnabled { get; set; } = false;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public float CameraMaxZoomDistance { get; set; } = 256;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public float CameraMinZoomDistance { get; set; } = 0.05f;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public float CharacterJumpHeight { get; set; } = 5;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public float CharacterJumpPower { get; set; } = 16;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public float CharacterMaxSlopeAngle { get; set; } = 1;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public bool CharacterUseJumpPower { get; set; } = true;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public float CharacterWalkSpeed { get; set; } = 16;
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public bool EnableMouseLockOption { get; set; } = true;

    public override string ClassName => nameof(StarterPlayer);

    public const ulong NETWORK_CONSTANT_ID = 14;

    public StarterPlayer(ulong id, GameManager gameManager) : base(NETWORK_CONSTANT_ID, gameManager)
    {
    }

    public override void CommitStageInitialize()
    {
        base.CommitStageInitialize();

        if (FindFirstChildOfClass(nameof(StarterCharacterScripts)) == null)
        {
            StarterCharacterScripts scs = GameManager.GameRegistry.Construct<StarterCharacterScripts>();
            scs.Parent = this;
            scs.CommitStageInitialize();
        }
        if (FindFirstChildOfClass(nameof(StarterPlayerScripts)) == null)
        {
            StarterPlayerScripts sps = GameManager.GameRegistry.Construct<StarterPlayerScripts>();
            sps.Parent = this;
            sps.CommitStageInitialize();
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
        if (className != nameof(StarterPlayer))
            return base.IsA(className);
        return true;
    }
}