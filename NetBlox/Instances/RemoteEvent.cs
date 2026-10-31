using MoonSharp.Interpreter;
using NetBlox.Instances.Services;
using NetBlox.Network;
using NetBlox.Runtime;

namespace NetBlox.Instances;

[Creatable]
[ExplorerIconIndex(75)]
public class RemoteEvent : Instance
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public LuaEvent OnServerEvent { get; set; } = new LuaEvent();
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public LuaEvent OnClientEvent { get; set; } = new LuaEvent();

    public override string ClassName => nameof(RemoteEvent);

    public RemoteEvent(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public void FireClient(Player player, params DynValue[] arguments)
    {
        FireClients([player], arguments);
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public void FireAllClients(params DynValue[] arguments)
    {
        Players players = Root.GetService<Players>();
        FireClients(players.GetPlayers(), arguments);
    }

    private void FireClients(Player[] players, params DynValue[] arguments)
    {
        if (GameManager.AccessFlags.HasFlag(AccessFlags.DisableNetworking))
            throw new InvalidOperationException("Cannot use RemoteEvent due to a policy");
        if (GameManager.NetworkMode != NetworkMode.Server)
            throw new InvalidOperationException("Cannot call FireClient/FireAllClients on a server");

        DynValue packed = DynValue.NewTable(GameManager.GetCurrentLuaState());

        foreach (DynValue argument in arguments)
            packed.Table.Append(argument);

        NetworkPacket networkPacket = NPRemoteEvent.Create(this, packed);
        
        foreach (Player player in players)
        {
            if (player.Connection != null)
                player.Connection.SendPacketReliable(networkPacket);
        }
    }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public void FireServer(params DynValue[] arguments)
    {
        if (GameManager.AccessFlags.HasFlag(AccessFlags.DisableNetworking))
            throw new InvalidOperationException("Cannot use RemoteEvent due to a policy");
        if (GameManager.NetworkMode != NetworkMode.Client)
            throw new InvalidOperationException("Cannot call FireServer on a server");

        NetworkClient networkClient = Root.GetService<NetworkClient>();

        if (networkClient.CurrentServerConnection == null)
            return; // let's be nice

        DynValue packed = DynValue.NewTable(GameManager.GetCurrentLuaState());

        foreach (DynValue argument in arguments)
            packed.Table.Append(argument);

        NetworkPacket networkPacket = NPRemoteEvent.Create(this, packed);
        networkClient.CurrentServerConnection.SendPacketReliable(networkPacket);
    }

    public override bool IsA(string className)
    {
        if (className != nameof(RemoteEvent))
            return base.IsA(className);
        return true;
    }
}