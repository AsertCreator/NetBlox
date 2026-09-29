using System.Reflection.Metadata;

namespace NetBlox.Network;

public sealed class NPKick : NetworkPacketHandler
{
    public static NetworkPacket Create(string message)
    {
        using MemoryStream ms = new MemoryStream();
        using BinaryWriter bw = new BinaryWriter(ms);

        bw.Write(message);

        return new NetworkPacket()
        {
            Type = (int)NetworkPacketType.NPKick,
            Payload = ms.ToArray()
        };
    }

    public override void HandleServerboundPacket(NetworkPacket networkPacket, GameManager gm, Player player)
    {
        NetworkServer networkServer = gm.RootModel.GetService<NetworkServer>();
        networkServer.ReportInvalidPacket(networkPacket, player);
    }
    public override void HandleClientboundPacket(NetworkPacket networkPacket, GameManager gm, CompoundConnection connection)
    {
        using MemoryStream ms = new MemoryStream(networkPacket.Payload);
        using BinaryReader br = new BinaryReader(ms);

        string message = br.ReadString();

        gm.TryGetEventForId(GameEvent.EVENT_KICKED)?.Fire(message);
    }
}