using NetBlox.Runtime;

namespace NetBlox.Instances.Services;

[Service]
[ReplicateChildren]
public class ReplicatedStorage : Instance
{
    public override string ClassName => nameof(ReplicatedStorage);

    public const ulong NETWORK_CONSTANT_ID = 6;

    public ReplicatedStorage(ulong id, GameManager gameManager) : base(NETWORK_CONSTANT_ID, gameManager)
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
        if (className != nameof(ReplicatedStorage))
            return base.IsA(className);
        return true;
    }
}