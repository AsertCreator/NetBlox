using NetBlox.Runtime;

using NetBlox.Instances.Parts;

namespace NetBlox.Instances.Services;

[Service]
[ReplicateChildren]
public class Workspace : Model
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public float FallenPartsDestroyHeight { get; set; } = -50;

    public override string ClassName => nameof(Workspace);

    public const ulong NETWORK_CONSTANT_ID = 2;

    public Workspace(ulong id, GameManager gameManager) : base(NETWORK_CONSTANT_ID, gameManager)
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
        if (className != nameof(Workspace))
            return base.IsA(className);
        return true;
    }
}