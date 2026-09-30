namespace NetBlox.Network;

public sealed class NPPing : NetworkPacketHandler
{
    public static NetworkPacket Create(int pingvalue)
    {
        using MemoryStream ms = new MemoryStream();
        using BinaryWriter bw = new BinaryWriter(ms);

        bw.Write(pingvalue);

        return new NetworkPacket()
        {
            Type = (int)NetworkPacketType.NPPing,
            Payload = ms.ToArray()
        };
    }

    public override void HandleServerboundPacket(NetworkPacket networkPacket, GameManager gm, Player player)
    {
        using MemoryStream ms = new MemoryStream(networkPacket.Payload);
        using BinaryReader br = new BinaryReader(ms);

        int pingValue = br.ReadInt32();

        NetworkPacket packet = Create(-pingValue);
        player.Connection?.SendPacketReliable(packet);
    }
    public override void HandleClientboundPacket(NetworkPacket networkPacket, GameManager gm, CompoundConnection connection)
    {
        using MemoryStream ms = new MemoryStream(networkPacket.Payload);
        using BinaryReader br = new BinaryReader(ms);

        int pingValue = br.ReadInt32();

        NetworkPacket packet = Create(-pingValue);
        connection.SendPacketReliable(packet);
    }
}