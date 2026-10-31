using MoonSharp.Interpreter;
using NetBlox.Instances.Services;
using NetBlox.Network;
using NetBlox.Runtime;

namespace NetBlox.Instances;

[Creatable]
public class RemoteEvent : BaseRemoteEvent
{
    public override string ClassName => nameof(RemoteEvent);

    public RemoteEvent(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    protected override void SendPacketToPlayers(Player[] players, NetworkPacket networkPacket)
    {
        foreach (Player player in players)
        {
            if (player.Connection != null)
                player.Connection.SendPacketReliable(networkPacket);
        }
    }
    protected override void SendPacketToServer(NetworkPacket networkPacket)
    {
        NetworkClient networkClient = Root.GetService<NetworkClient>();
        networkClient.CurrentServerConnection?.SendPacketReliable(networkPacket);
    }

    public override bool IsA(string className)
    {
        if (className != nameof(RemoteEvent))
            return base.IsA(className);
        return true;
    }
}