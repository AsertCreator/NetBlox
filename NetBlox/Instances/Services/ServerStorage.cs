using NetBlox.Runtime;

namespace NetBlox.Instances.Services;

[Service]
[NotReplicated]
public class ServerStorage : Instance
{
    public override string ClassName => nameof(ServerStorage);

    public const ulong NETWORK_CONSTANT_ID = 8;

    public ServerStorage(ulong id, GameManager gameManager) : base(NETWORK_CONSTANT_ID, gameManager)
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
        if (className != nameof(ServerStorage))
            return base.IsA(className);
        return true;
    }
}