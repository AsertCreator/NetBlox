using NetBlox.Instances.Parts;
using NetBlox.Instances.UI;
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

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.WritePlayer)]
    public Player CreatePlayer(long userId, string name)
    {
        Player player = GameManager.GameRegistry.Construct<Player>();
        player.SetUserId(userId);
        player.Parent = this;
        player.Name = name;

        return player;
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public Player CreateLocalPlayer()
    {
        GameManager.GameScheduler.BeginTracedSecurityOverride(SecurityIdentity.SI_EngineNetworker, "Creating a Local Player");

        Player player = CreatePlayer(-Random.Shared.Next() - 1, "");

        StarterGui starterGui = Root.GetService<StarterGui>();
        PlayerGui playerGui = GameManager.GameRegistry.Construct<PlayerGui>();
        playerGui.Parent = player;
        
        Instance[] guiChildren = starterGui.GetChildren();
        for (int i = 0; i < guiChildren.Length; i++)
            guiChildren[i].Clone()?.Parent = playerGui;

        GameManager.GameScheduler.EndTracedSecurityOverride();

        return player;
    }

    [LuaAlias(Name = "players")]
    [LuaAlias(Name = "getPlayers")]
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public Player[] GetPlayers()
    {
        List<Player> players = new List<Player>();
        using RentedSpan<Instance?> children = GetChildren_Fast();
        for (int i = 0; i < children.Values.Length; i++)
        {
            if (children.Values[i] is Player player)
                players.Add(player);
        }
        return players.ToArray();
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public Player? GetPlayerFromUserId(long userId)
    {
        Player[] players = GetPlayers();
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].UserId == userId)
                return players[i];
        }
        return null;
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public Player? GetPlayerFromCharacter(Model? model)
    {
        if (model == null)
            return null;
        Player[] players = GetPlayers();
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].Character == model)
                return players[i];
        }
        return null;
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