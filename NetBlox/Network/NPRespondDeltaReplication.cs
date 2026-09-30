using NetBlox.Instances.Services;

namespace NetBlox.Network;

public sealed class NPRespondDeltaReplication : NetworkPacketHandler
{
    public record struct Entity
    {
        public int Flags;
        public byte[] Payload;
    }

    public static NetworkPacket Create(Entity entity)
    {
        using MemoryStream ms = new MemoryStream();
        using BinaryWriter bw = new BinaryWriter(ms);

        bw.Write(entity.Flags);
        bw.Write(entity.Payload.Length);
        bw.Write(entity.Payload);

        return new NetworkPacket()
        {
            Type = (int)NetworkPacketType.NPRespondDeltaReplication,
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

        int replicationFlags = br.ReadInt32();
        int payloadLength = br.ReadInt32();
        byte[] payload = br.ReadBytes(payloadLength);

        NetworkClient networkClient = gm.RootModel.GetService<NetworkClient>();
        ReplicationAgent replicationAgent = networkClient.ReplicationAgent;

        lock (replicationAgent.PendingDeltaReplicationFrames)
            replicationAgent.PendingDeltaReplicationFrames.Enqueue(payload);
    }
}