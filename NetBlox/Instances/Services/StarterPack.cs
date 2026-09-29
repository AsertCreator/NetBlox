using NetBlox.Runtime;

namespace NetBlox.Instances.Services;

[Service]
[ReplicateChildren]
public class StarterPack : Instance
{
    public override string ClassName => nameof(StarterPack);

    public const ulong NETWORK_CONSTANT_ID = 13;

    public StarterPack(ulong id, GameManager gameManager) : base(NETWORK_CONSTANT_ID, gameManager)
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
        if (className != nameof(StarterPack))
            return base.IsA(className);
        return true;
    }
}