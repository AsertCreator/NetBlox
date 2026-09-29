using NetBlox.Runtime;

namespace NetBlox.Instances.Services;

[ReplicateChildren]
public class StarterPlayerScripts : Instance
{
    public override string ClassName => nameof(StarterPlayerScripts);

    public const ulong NETWORK_CONSTANT_ID = 15;

    public StarterPlayerScripts(ulong id, GameManager gameManager) : base(NETWORK_CONSTANT_ID, gameManager)
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
        if (className != nameof(StarterPlayerScripts))
            return base.IsA(className);
        return true;
    }
}