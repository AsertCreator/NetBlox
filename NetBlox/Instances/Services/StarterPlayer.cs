using NetBlox.Runtime;

namespace NetBlox.Instances.Services;

[Service]
[ReplicateChildren]
public class StarterPlayer : Instance
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public bool AutoJumpEnabled { get; set; }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public float CameraMaxZoomDistance { get; set; }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public float CameraMinZoomDistance { get; set; }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public float CharacterJumpHeight { get; set; }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public float CharacterJumpPower { get; set; }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public float CharacterMaxSlopeAngle { get; set; }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public bool CharacterUseJumpPower { get; set; }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public float CharacterWalkSpeed { get; set; }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public bool EnableMouseLockOption { get; set; }

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