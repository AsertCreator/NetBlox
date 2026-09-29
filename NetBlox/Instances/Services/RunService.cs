using NetBlox.Runtime;

namespace NetBlox.Instances.Services;

[Service]
[NotReplicated]
public class RunService : Instance
{
    public override string ClassName => nameof(RunService);

    public const ulong NETWORK_CONSTANT_ID = 18;

    public RunService(ulong id, GameManager gameManager) : base(NETWORK_CONSTANT_ID, gameManager)
    {
        Name = "Run Service";
    }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public bool IsRunMode()
    {
        return GameManager.EditorMode.IsInRunMode;
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public bool IsEdit()
    {
        return GameManager.EditorMode.IsInEditMode;
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public bool IsStudio()
    {
        return GameManager.EditorMode.IsStudio;
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public bool IsClient()
    {
        return GameManager.NetworkMode == Network.NetworkMode.Client;
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public bool IsServer()
    {
        return GameManager.NetworkMode == Network.NetworkMode.Server;
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public bool IsRunning()
    {
        return GameManager.ScriptsRunning;
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxEngine)]
    public void Pause()
    {
        Stop();
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxEngine)]
    public void Stop()
    {
        GameManager.PhysicsSolver?.CanRun = false;
        GameManager.ScriptsRunning = false;
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxEngine)]
    public void Run()
    {
        GameManager.PhysicsSolver?.CanRun = true;
        GameManager.ScriptsRunning = true;
    }

    public override void Destroy()
    {
        if (!GameManager.GameScheduler.GetCurrentSecurityIdentity()!.RequireSimpleCapability(SimpleSecurityCapabilityLevel.DestroyServices))
            return;
        base.Destroy();
    }

    public override bool IsA(string className)
    {
        if (className != nameof(RunService))
            return base.IsA(className);
        return true;
    }
}