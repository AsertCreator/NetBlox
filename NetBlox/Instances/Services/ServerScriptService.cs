using NetBlox.Runtime;

namespace NetBlox.Instances.Services;

[Service]
[NotReplicated]
public class ServerScriptService : Instance
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.Plugin)]
    public bool LoadStringEnabled { get; set; }

    public override string ClassName => nameof(ServerScriptService);

    public const ulong NETWORK_CONSTANT_ID = 7;

    public ServerScriptService(ulong id, GameManager gameManager) : base(NETWORK_CONSTANT_ID, gameManager)
    {
    }

    public override void Destroy()
    {
        if (!GameManager.GameScheduler.GetCurrentSecurityIdentity()!.RequireSimpleCapability(SimpleSecurityCapabilityLevel.DestroyServices))
            return;
        base.Destroy();
    }

    public override bool IsA(string className)
    {
        if (className != nameof(ServerScriptService))
            return base.IsA(className);
        return true;
    }
}