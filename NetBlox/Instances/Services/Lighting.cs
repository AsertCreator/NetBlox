using System.Numerics;
using NetBlox.Runtime;

namespace NetBlox.Instances.Services;

[Service]
[ReplicateChildren]
public class Lighting : Instance
{

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public Vector3 SunPosition { get; set; } = new Vector3(0, 70, -40);
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public bool RealTimeShadows { get; set; } = true;
    
    public override string ClassName => nameof(Lighting);

    public const ulong NETWORK_CONSTANT_ID = 3;

    public Lighting(ulong id, GameManager gameManager) : base(NETWORK_CONSTANT_ID, gameManager)
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
        if (className != nameof(Lighting))
            return base.IsA(className);
        return true;
    }
}