using NetBlox.Network;
using NetBlox.Runtime;

namespace NetBlox.Instances.Services;

[Service]
[ReplicateChildren]
public class Players : Instance
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public Player? LocalPlayer
    {
        get
        {
            if (GameManager.NetworkMode == NetworkMode.Server)
                return null;
            return field;
        }
        private set;
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public LuaEvent PlayerAdded { get; } = new LuaEvent();
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public LuaEvent PlayerRemoving { get; } = new LuaEvent();

    public override string ClassName => nameof(Players);

    public const ulong NETWORK_CONSTANT_ID = 4;

    public Players(ulong id, GameManager gameManager) : base(NETWORK_CONSTANT_ID, gameManager)
    {
    }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxEngine)]
    public void SetLocalPlayer(Player player)
    {
        LocalPlayer = player;
    }

    public override void Destroy()
    {
        if (!GameManager.GameScheduler.GetCurrentSecurityIdentity()!.RequireSimpleCapability(SimpleSecurityCapabilityLevel.DestroyServices))
            return;
        base.Destroy();
    }
    public override bool AskToBeParent(Instance child)
    {
        if (child is not Player)
            return false;
        return true;
    }

    public override bool IsA(string className)
    {
        if (className != nameof(Players))
            return base.IsA(className);
        return true;
    }
}