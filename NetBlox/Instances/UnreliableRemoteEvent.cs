using MoonSharp.Interpreter;
using NetBlox.Instances.Services;
using NetBlox.Network;
using NetBlox.Runtime;

namespace NetBlox.Instances;

[Creatable]
public class UnreliableRemoteEvent : BaseRemoteEvent
{
    public override string ClassName => nameof(UnreliableRemoteEvent);

    public UnreliableRemoteEvent(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    protected override void SendPacketToPlayers(Player[] players, NetworkPacket networkPacket)
    {
        foreach (Player player in players)
        {
            if (player.Connection != null)
                player.Connection.SendPacketUnreliable(networkPacket);
        }
    }
    protected override void SendPacketToServer(NetworkPacket networkPacket)
    {
        NetworkClient networkClient = Root.GetService<NetworkClient>();
        networkClient.CurrentServerConnection?.SendPacketUnreliable(networkPacket);
    }

    public override bool IsA(string className)
    {
        if (className != nameof(UnreliableRemoteEvent))
            return base.IsA(className);
        return true;
    }
}