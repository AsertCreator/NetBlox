using NetBlox.Runtime;

namespace NetBlox.Instances.Services.Internal;

/// <summary>
/// A Service similar to ReplicatedStorage but only for CoreScripts. Usually stuff that is in there is events and functions and only work if both
/// Server and Client CoreScript's are loaded. But even then only they can access the thing so eh
/// </summary>
[Service]
[ServiceAccessibleTo(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
[ReplicateChildren]
public class CoreReplicatedStorage : Instance
{
    public override string ClassName => nameof(CoreReplicatedStorage);

    public const ulong NETWORK_CONSTANT_ID = 106;

    public CoreReplicatedStorage(ulong id, GameManager gameManager) : base(NETWORK_CONSTANT_ID, gameManager)
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
        if (className != nameof(CoreReplicatedStorage))
            return base.IsA(className);
        return true;
    }
}