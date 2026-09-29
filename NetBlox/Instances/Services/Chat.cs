using NetBlox.Runtime;

namespace NetBlox.Instances.Services;

[Service]
[ReplicateChildren]
public class Chat : Instance
{
    public override string ClassName => nameof(Chat);

    public const ulong NETWORK_CONSTANT_ID = 9;

    public Chat(ulong id, GameManager gameManager) : base(NETWORK_CONSTANT_ID, gameManager)
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
        if (className != nameof(Chat))
            return base.IsA(className);
        return true;
    }
}