using NetBlox.Runtime;

namespace NetBlox.Instances.Services;

[ReplicateChildren]
public class StarterCharacterScripts : StarterPlayerScripts
{
    public override string ClassName => nameof(StarterCharacterScripts);

    public new const ulong NETWORK_CONSTANT_ID = 16;

    public StarterCharacterScripts(ulong id, GameManager gameManager) : base(NETWORK_CONSTANT_ID, gameManager)
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
        if (className != nameof(StarterCharacterScripts))
            return base.IsA(className);
        return true;
    }
}