using NetBlox.Runtime;

namespace NetBlox.Instances.Services;

[Service]
[ReplicateChildren]
public class ReplicatedFirst : Instance
{
    public override string ClassName => nameof(ReplicatedFirst);

    public const ulong NETWORK_CONSTANT_ID = 5;

    public ReplicatedFirst(ulong id, GameManager gameManager) : base(NETWORK_CONSTANT_ID, gameManager)
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
        if (className != nameof(ReplicatedFirst))
            return base.IsA(className);
        return true;
    }
}